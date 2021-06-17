// can't yet format YamlFrontmatter (["category: Documentation"; "categoryindex: 1"; "index: 3"], Some { StartLine = 2 StartColumn = 0 EndLine = 5 EndColumn = 8 }) to pynb markdown

[![Binder](img/badge-binder.svg)](https://mybinder.org/v2/gh/diffsharp/diffsharp.github.io/master?filepath=content.ipynb)&emsp;
[![Script](img/badge-script.svg)](content.fsx)&emsp;
[![Notebook](img/badge-notebook.svg)](content.ipynb)

# Creating Content

The ["fsdocs" tool](commandline.html) allows documentation for a site to be built
from content in a `docs` directory. The expected structure for a `docs` directory is

// can't yet format InlineHtmlBlock ("docs/**/*.md                  -- markdown with embedded code, converted to html and optionally tex/ipynb
docs/**/*.fsx                 -- fsx scripts converted to html and optionally tex/ipynb
docs/**/*                     -- other content, copied over
docs/**/_template.html        -- optional template, specifies the HTML template for this directory and its contents
docs/**/_template.tex         -- optionally indicates Latex files should be generated
docs/**/_template.ipynb       -- optionally indicates F# ipynb files should be generated
docs/**/_template.fsx         -- optionally indicates F# fsx files should be generated (even from markdown)
docs/reference/_template.html -- optionally specifies the default template for reference docs
", None, None) to pynb markdown

Processing is by these two commands:

// can't yet format InlineHtmlBlock ("dotnet fsdocs build
dotnet fsdocs watch
", None, None) to pynb markdown

The output goes in `output/` by default.  Processing is recursive, making this a form of static site generation.

## Literate Scripts and Markdown Content

The input directory may contain [literate scripts](literate.html) and markdown content.

## Other Content

Content that is not `*.fsx` or `*.md` is copied across.

## Default Styling Content

By default additional content such as `fsdocs-search.js`, `fsdocs-tips.js` and `fsdocs-styles.css` are included in the
the `content` directory of the output.  This can be suppressed with `--nodefaultcontent` or by having your own
copy of this content in your `content` directory.

## Ignored Content

Any file or directory beginning with `.` is ignored.

## Front matter

Each content file can have optional frontmatter.  This determines the navigation bar title, categorization and ordering.

For markdown, the format is:

// can't yet format InlineHtmlBlock ("---
title: Some Title
category: Some Category
categoryindex: 2
index: 3
---
", None, None) to pynb markdown

For F# scripts the frontmatter is in this form:

// can't yet format InlineHtmlBlock ("(**
---
title: A Literate Script
category: Examples
categoryindex: 2
index: 1
---
*)
", None, None) to pynb markdown

All entries are optional.
The `categoryindex` determines the ordering of categories.
The `index` determines the ordering of within each category.
The `title` is used in the navigation bar instead of any title inferred from the document.

## Multi-language Content

Versions of content in other languages should go in two-letter coded sub-directories, e.g.

// can't yet format InlineHtmlBlock ("docs/ja/...
docs/de/...
", None, None) to pynb markdown

These will be elided from the main table-of-contents and search indexes.  (Currently no language-specific
table of contents is built, nor language-specific site search indexes).

## Templates and Substitutions

Templates are used for HTML (`_template.html`), LaTeX (`_template.tex`), Notebooks (`_template.ipynb)`
and F# script outputs (`_template.fsx`).

The following substitutions determine the primary (non-styling) content of your site.
For example `{{fsdocs-content}}` is replaced with the generated content in each file.

Substitutions are applied when generating content from HTML templates, IPYNB templates, FSX templates.
They are also applied to content apart from Markdown inline code ``...``, Markdown LaTeX and
generated outputs.

See [Styling](styling.html) for information about template parameters and styling beyond the default template.

Substitution name | Generated content
:--- | :---
`root` | `<PackageProjectUrl>` else `/` followed by `fsdocs-collection-name`
`fsdocs-collection-name` | Name of .sln, single .fsproj or containing directory
`fsdocs-content` | Main page content
`fsdocs-list-of-namespaces` | HTML `<li>` list of namespaces with links
`fsdocs-list-of-documents` | HTML `<li>` list of documents with  titles and links
`fsdocs-page-title` | First h1 heading in literate file. Generated for API docs
`fsdocs-source` | Original literate script or markdown source
`fsdocs-source-filename` | Name of original input source, relative to the `docs` root
`fsdocs-source-basename` | Name of original input source, excluding its extensions, relative to the `docs` root
`fsdocs-tooltips` | Generated hidden div elements for tooltips
`fsdocs-watch-script` | The websocket script used in watch mode to trigger hot reload


The following substitutions are extracted from your project files and may or may not be used by the default
template:

Substitution name | Value
:--- | :---
`fsdocs-copyright` | `<Copyright>`
`fsdocs-package-project-url` | `<PackageProjectUrl>`
`fsdocs-package-license-expression` | `<PackageLicenseExpression>`
`fsdocs-package-tags` | `<PackageTags>`
`fsdocs-package-version` | `<Version>`


For the `fsdocs` tool, additional substitutions can be specified using `--parameters`.

## Cross References to API Docs

Markdown content can contain cross-references to API Docs.  Use inline
markdown code snippets of the special form ``cref:T:MyNamespace.MyType`` where `T:MyNamespace.MyType`
is a method, property or type xml doc sig reference, see [API Docs](apidocs.html).
This can include any cross-references resolved by fsdocs.

The generated API documentation includes buttons to copy the XML and Markdown forms of API doc references.

For example, within this project,

the text ``cref:T:FSharp.Formatting.Markdown.MarkdownParagraph`` resolves to the link [MarkdownParagraph](https://fsprojects.github.io/FSharp.Formatting/reference/fsharp-formatting-markdown-markdownparagraph.html)
the text ``cref:T:System.Console`` resolves to the link [Console](https://docs.microsoft.com/dotnet/api/system.console)
the text ``cref:M:System.Console.WriteLine`` resolves to the link [Console.WriteLine](https://docs.microsoft.com/dotnet/api/system.console.writeline)
the text ``cref:M:System.Console.WriteLine(System.String)`` resolves to the link [Console.WriteLine](https://docs.microsoft.com/dotnet/api/system.console.writeline)
the text ```cref:T:FSharp.Control.FSharpAsync`1``` resolves to the link [Async](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-control-fsharpasync-1)
the text ``cref:T:FSharp.Control.FSharpAsync`` resolves to the link [Async](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-control-fsharpasync)
the text ```cref:T:FSharp.Core.array`1``` resolves to the link [array](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-core-array-1)
the text ``cref:T:FSharp.Core.OptionModule`` resolves to the link [Option](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-core-optionmodule)
the text ````cref:M:FSharp.Collections.ListModule.Append``1```` resolves to the link [ListModule.Append](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-listmodule#Append)
// can't yet format QuotedBlock ([Paragraph ([Literal ("NOTE: These cases act as tests - if the links above do not work, then that indicates a bug or a change in the
external link. ", Some { StartLine = 158 StartColumn = 2 EndLine = 159 EndColumn = 127 }); DirectLink ([Literal ("Please report it", Some { StartLine = 158 StartColumn = 127 EndLine = 159 EndColumn = 143 })], "https://github.com/fsprojects/FSharp.Formatting/issues/new", None, Some { StartLine = 158 StartColumn = 127 EndLine = 159 EndColumn = 96 }); Literal (".", Some { StartLine = 158 StartColumn = 127 EndLine = 159 EndColumn = 128 })], Some { StartLine = 158 StartColumn = 2 EndLine = 158 EndColumn = 111 })], Some { StartLine = 158 StartColumn = 0 EndLine = 158 EndColumn = 111 }) to pynb markdown

Determining xmldoc sig references is not simple.  The API doc generated pages come with
buttons to copy out the XmlDoc signature.

## Generating HTML Output

HTML is generated by default. You can also add a `_template.html`.  This should contain `{{fsdocs-content}}`,  `{{fsdocs-tooltips}}`
and other placeholders. Substitutions are
applied to this template.
If a file `_template.html` exists then is used as the template for HTML generation for that directory and all sub-content.

## Generating LaTeX output

To generate .tex output for each script and markdown file, add a `_template.tex`. Substitutions are
applied to this template. The file is either empty of contains `{{fsdocs-content}}` as the key where the body
of the document is placed.

## Generating iPython Notebook output

To generate .ipynb output for each script and markdown file, add a `_template.ipynb`, usually empty. Substitutions are
applied to this template.

To add a `mybinder` badge to your generated notebook, ensure you have a `Dockerfile` and `NuGet.config`
in your `docs` directory and use text like this:

// can't yet format InlineHtmlBlock ("[![Binder](https://mybinder.org/badge_logo.svg)](https://mybinder.org/v2/gh/fsprojects/FSharp.Formatting/gh-pages?filepath=literate.ipynb)
", None, None) to pynb markdown

## Generating Script outputs

To generate .fsx output for each script and markdown file, add a `_template.fsx`, usually empty. Substitutions are
applied to this template. It is either empty of contains `{{fsdocs-content}}` as the key where the body
of the script is placed.


