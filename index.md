# F# Formatting: Documentation Tools for F# Code

FSharp.Formatting is a set of libraries and tools for processing F# script files, markdown and for
generating API documentation. F# Formatting package is used by this project and many other repositories.

To use the tool, install and use the [fsdocs](commandline.html) tool in a typical F# project with
F# project files plus markdown and script content in the `docs` directory:

// can't yet format InlineHtmlBlock ("dotnet tool install FSharp.Formatting.CommandTool
dotnet fsdocs build 
dotnet fsdocs watch
", None, None) to pynb markdown

To use the tool, explore the following topics:

[Authoring Content](content.html) - explains the content expected in the `docs` directory for the `fsdocs` tool.
[Using the Command line tool](commandline.html) - explains how to use the `fsdocs` tool.
[Generating API documentation](apidocs.html) - how to generate HTML documentation
from "XML comments" in your .NET libraries. The tool handles comments written in
Markdown too.
[Styling](styling.html) - explains some options for styling the output of `fsdocs`.
[Using literate programming](literate.html) - explains how to generate documentation
for your projects or to write nicely formatted F# blog posts.
[Embedding F# outputs in literate programming](evaluation.html) - provides more details on literate programming and
explains how to embed results of a literate script file in the generated output. This way,
you can easily format the results of running your code!
## Using FSharp.Formatting as a library

F# Formatting is also [available on NuGet](https://nuget.org/packages/FSharp.Formatting) as a set of libraries.

[Markdown parser](markdown.html) - explains the F# Markdown
processor that is available in this library with some basic examples of
document processing.
[F# code formatting](codeformat.html) - more details about the F# code
formatter and how to use it to obtain information about F# source files.
## More information

The documentation for this library is generated automatically using the tools
built here. If you spot a typo, please submit a pull request! The source Markdown and F# script files are
available in the [docs folder on GitHub](https://github.com/fsprojects/FSharp.Formatting/tree/master/docs).

The project is hosted on [GitHub](https://github.com/fsprojects/FSharp.Formatting) where you can
[report issues](https://github.com/fsprojects/FSharp.Formatting/issues), fork the project and submit pull requests.
See the  [License file](https://github.com/fsprojects/FSharp.Formatting/blob/master/LICENSE.md) in the GitHub repository.


