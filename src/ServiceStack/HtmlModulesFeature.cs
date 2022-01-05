#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.HtmlModules;
using ServiceStack.Host.Handlers;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

/// <summary>
/// Simple, lightweight & high-performant HTML templating solution 
/// </summary>
public class HtmlModulesFeature : IPlugin, Model.IHasStringId
{
    public string Id => "module:" + string.Join(",", Modules.Select(x => x.BasePath).ToArray());
    
    public bool IgnoreIfError { get; set; }

    /// <summary>
    /// Define literal tokens to be replaced with dynamic fragments, e.g:
    /// &lt;base href=""&gt; = ctx => $"&lt;base href=\"{ctx.Request.ResolveAbsoluteUrl($"~{DirPath}/")}\"&gt;"
    /// </summary>
    public Dictionary<string, Func<HtmlModuleContext, ReadOnlyMemory<byte>>> Tokens { get; set; } = new();

    /// <summary>
    /// Define custom html handlers, e.g:
    /// &lt;!--shared:Brand,Input--&gt;
    /// &lt;!--file:/path/to/single.html--&gt; or /*file:/path/to/single.txt*/
    /// &lt;!--files:/dir/components/*.html--&gt; or /*files:/dir/*.css*/
    /// </summary>
    public List<IHtmlModulesHandler> Handlers { get; set; } = new()
    {
        new FileHandler("file"),
        new FilesHandler("files"),
    };
    
    public HtmlModule[] Modules { get; }
    public HtmlModulesFeature(params HtmlModule[] modules) => Modules = modules;
    public Action<HtmlModule>? Configure { get; set; }
    public IVirtualPathProvider? VirtualFiles { get; set; }

    public void Register(IAppHost appHost)
    {
        VirtualFiles ??= appHost.VirtualFiles;
        foreach (var component in Modules)
        {
            component.Feature = this;
            component.VirtualFiles ??= VirtualFiles;
            Configure?.Invoke(component);
            component.Register(appHost);
        }
    }
}

public class HtmlModuleContext
{
    public HtmlModule Module { get; }
    public IRequest Request { get; }
    public IVirtualPathProvider VirtualFiles => Module.VirtualFiles!;

    public HtmlModuleContext(HtmlModule module, IRequest request)
    {
        Module = module;
        Request = request;
    }

    public ReadOnlyMemory<byte> Cache(string key, Func<string, ReadOnlyMemory<byte>> handler)
    {
        if (!HostContext.DebugMode)
            return Module.Cache.GetOrAdd(key, handler);
        
        return handler(key);
    }
}

public class HtmlModule
{
    public HtmlModulesFeature? Feature { get; set; }
    public string DirPath { get; set; }
    public string BasePath { get; set; }
    public IVirtualPathProvider? VirtualFiles { get; set; }

    public string IndexFile { get; set; } = "index.html";
    public List<string> PublicPaths { get; set; } = new() {
        "/assets"
    };

    public Dictionary<string, Func<HtmlModuleContext, ReadOnlyMemory<byte>>> Tokens { get; set; }
    public List<IHtmlModulesHandler> Handlers { get; set; } = new();

    public HtmlModule(string dirPath, string? basePath=null)
    {
        DirPath = dirPath.TrimEnd('/');
        BasePath = (basePath ?? DirPath).TrimEnd('/');
        Tokens = new() {
            ["<base href=\"\">"] = ctx => ($"<base href=\"{ctx.Request.ResolveAbsoluteUrl($"~{BasePath}/")}\">"
            + (HostContext.DebugMode ? "\n<script src=\"/js/hot-fileloader.js\"></script>" : "")).AsMemory().ToUtf8(),
        };
    }
    
    public ConcurrentDictionary<string, ReadOnlyMemory<byte>> Cache { get; } = new();

    struct FragmentTuple
    {
        internal int index;
        internal string token;
        internal IHtmlModuleFragment fragment;
        public FragmentTuple(int index, string token, IHtmlModuleFragment fragment)
        {
            this.index = index;
            this.token = token;
            this.fragment = fragment;
        }
    };

    IHtmlModuleFragment[]? indexFragments;
    IHtmlModuleFragment[] GetIndexFragments()
    {
        if (!HostContext.DebugMode && indexFragments != null)
            return indexFragments;

        var indexFile = VirtualFiles!.GetFile(DirPath.CombineWith(IndexFile));
        if (indexFile == null)
        {
            if (Feature!.IgnoreIfError) 
                return TypeConstants<IHtmlModuleFragment>.EmptyArray;
            throw HttpError.NotFound(DirPath.CombineWith(IndexFile) + " was not found");
        }
        var indexContents = indexFile.ReadAllText().AsMemory();
        
        var fragmentDefs = new List<FragmentTuple>();
        foreach (var entry in Tokens.Union(Feature?.Tokens ?? new()))
        {
            var pos = indexContents.IndexOf(entry.Key);
            if (pos == -1) continue;
            fragmentDefs.Add(new(pos, entry.Key, new HtmlTokenFragment(entry.Key, entry.Value)));
        }
        foreach (var handler in Handlers.Union(Feature?.Handlers ?? new()))
        {
            var htmlCommentPrefix = "<!--" + handler.Name + ":";
            var pos = indexContents.IndexOf(htmlCommentPrefix);
            if (pos >= 0)
            {
                var endPos = indexContents.IndexOf("-->", pos);
                if (endPos == -1)
                    throw new Exception($"{htmlCommentPrefix} is missing -->");
                var token = indexContents.Slice(pos, (endPos - pos) + "-->".Length).ToString();
                var args = token.Substring(htmlCommentPrefix.Length, token.Length - htmlCommentPrefix.Length - "-->".Length);
                fragmentDefs.Add(new(pos, token, new HtmlHandlerFragment(token, args, handler.Execute)));
            }

            var jsCommentPrefix = "/*" + handler.Name + ":";
            pos = indexContents.IndexOf(jsCommentPrefix);
            if (pos >= 0)
            {
                var endPos = indexContents.IndexOf("*/", pos);
                if (endPos == -1)
                    throw new Exception($"{jsCommentPrefix} is missing */");
                var token = indexContents.Slice(pos, endPos - pos + "-->".Length).ToString();
                var args = token.Substring(jsCommentPrefix.Length, token.Length - jsCommentPrefix.Length - "*/".Length);
                fragmentDefs.Add(new(pos, token, new HtmlHandlerFragment(token, args, handler.Execute)));
            }
        }
        fragmentDefs.Sort((a, b) => a.index.CompareTo(b.index));

        var fragments = new List<IHtmlModuleFragment>();
        var lastPos = 0;
        for (var i = 0; i < fragmentDefs.Count; i++)
        {
            var fragmentDef = fragmentDefs[i];
            var startPos = indexContents.IndexOf(fragmentDef.token);
            if (startPos == -1)
                throw new Exception($"Error parsing {IndexFile}, missing '{fragmentDef.token}'");
            
            fragments.Add(new HtmlTextFragment(indexContents.Slice(lastPos, startPos - lastPos)));
            fragments.Add(fragmentDef.fragment);
            
            lastPos = startPos + fragmentDef.token.Length;
        }
        fragments.Add(new HtmlTextFragment(indexContents.Slice(lastPos)));

        indexFragments = fragments.ToArray();
        return indexFragments;
    }

    public void Register(IAppHost appHost)
    {
        VirtualFiles ??= appHost.VirtualFiles;
        var fragments = GetIndexFragments(); //force parsing
        if (fragments.Length == 0) //Feature.IgnoreIfError
            return;

        appHost.RawHttpHandlers.Add(req =>
        {
            if (!req.PathInfo.StartsWith(BasePath))
                return null;
            
            foreach (var path in PublicPaths)
            {
                if (req.PathInfo.StartsWith(BasePath + path))
                {
                    var file = VirtualFiles.GetFile(DirPath + req.PathInfo.Substring(BasePath.Length));
                    return file != null
                        ? new StaticFileHandler(file)
                        : new NotFoundHttpHandler();
                }
            }

            return new CustomActionHandlerAsync(async (httpReq, httpRes) =>
            {
                var fragments = GetIndexFragments();
                var ms = MemoryStreamFactory.GetStream();
                var ctx = new HtmlModuleContext(this, httpReq);
                foreach (var fragment in fragments)
                {
                    await fragment.WriteToAsync(ctx, ms).ConfigAwait();
                }
                httpRes.ContentType = MimeTypes.Html;
                ms.Position = 0;
                await ms.CopyToAsync(httpRes.OutputStream).ConfigAwait();
            });
        });
    }
}