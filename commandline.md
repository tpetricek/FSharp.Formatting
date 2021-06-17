// can't yet format YamlFrontmatter (["category: Documentation"; "categoryindex: 1"; "index: 1"], Some { StartLine = 1 StartColumn = 0 EndLine = 4 EndColumn = 8 }) to pynb markdown

# Command line

To use F# Formatting tools via the command line, you can use the `fsdocs` dotnet tool.

// can't yet format InlineHtmlBlock ("dotnet tool install FSharp.Formatting.CommandTool
dotnet fsdocs [command] [options]
", None, None) to pynb markdown

## The build command

This command processes a `docs` directory and generates API docs for projects in the solution according to the
rules of [API doc generation](apidocs.html). The input accepted is described in [content](content.html).

// can't yet format InlineHtmlBlock ("fsdocs build
", None, None) to pynb markdown

The command line options accepted are:

Command Line Option | Description
:--- | :---
`--input` | Input directory of content (default: `docs`)
`--projects` | Project files to build API docs for outputs, defaults to all packable projects
`--output` | Output Directory (default 'output' for 'build' and 'tmp/watch' for 'watch')
`--noapidocs` | Disable generation of API docs
`--eval` | Evaluate F# fragments in scripts
`--saveimages` | Save images referenced in docs
`--nolinenumbers` | Don't add line numbers, default is to add line number.
`--parameters` | Additional substitution parameters for templates
`--nonpublic` | The tool will also generate documentation for non-public members
`--nodefaultcontent` | Do not copy default content styles, javascript or use default templates
`--clean` | Clean the output directory
`--help` | Display this help screen
`--version` | Display version information
`--properties` | Provide properties to dotnet msbuild, e.g. --properties Configuration=Release Version=3.4
`--strict` | Fail if docs are missing or can't be generated


The following command line options are also accepted but it is instead recommended you use
settings in your .fsproj project files:

Command Line Option | Description
:--- | :---
`--sourcefolder` | Source folder at time of component build (`<FsDocsSourceFolder>`)
`--sourcerepo` | Source repository for github links (`<FsDocsSourceRepository>`)
`--mdcomments` | Assume comments in F# code are markdown (`<UsesMarkdownComments>`)


## The watch command

This command does the same as `fsdocs build` but in "watch" mode, waiting for changes. Only the files in the input
directory (e.g. `docs`) are watched.

// can't yet format InlineHtmlBlock ("fsdocs watch
", None, None) to pynb markdown

Restarting may be necesssary on changes to project files. The same parameters are accepted, plus these:

Command Line Option | Description
:--- | :---
`--noserver` | Do not serve content when watching.
`--nolaunch` | Do not launch a browser window.
`--open` | URL extension to launch [http://localhost:<port>/%s.](http://localhost:<port>/%s.)
`--port` | Port to serve content for [http://localhost](http://localhost) serving.


## Searchable docs

When using the command-line tool a Lunr search index is automatically generated in `index.json`.

A search box is included in the default template.  To add a search box
to your own `_template.html`, include `fsdocs-search.js`, which is added to the `content`
by default.

// can't yet format InlineHtmlBlock ("...
<div id="header">
  <div class="searchbox">
    <label for="search-by">
      <i class="fas fa-search"></i>
    </label>
    <input data-search-input="" id="search-by" type="search" placeholder="Search..." />
    <span data-search-clear="">
      <i class="fas fa-times"></i>
    </span>
  </div>
</div>
...
", None, None) to pynb markdown


