// can't yet format YamlFrontmatter (["title: Generating API Docs"; "category: Documentation"; "categoryindex: 1"; "index: 5"], Some { StartLine = 2 StartColumn = 0 EndLine = 6 EndColumn = 8 }) to pynb markdown

[![Binder](img/badge-binder.svg)](https://mybinder.org/v2/gh/diffsharp/diffsharp.github.io/master?filepath=apidocs.ipynb)&emsp;
[![Script](img/badge-script.svg)](apidocs.fsx)&emsp;
[![Notebook](img/badge-notebook.svg)](apidocs.ipynb)

# API Documentation Generation

The [command-line tool `fsdocs`](commandline.html) can be used to generate documentation
for F# libraries with XML comments.  The documentation is normally built using `fsdocs build` and developed using `fsdocs watch`. For
the former the output will be placed in `output\reference` by default.

## Templates

The HTML is built by instantiating a template. The template used is the first of:

`docs/reference/_template.html`
`docs/_template.html`
The default template
Usually the same template can be used as for [other content](content.html).

## Classic XML Doc Comments

XML Doc Comments may use [the normal F# and C# XML doc standards](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/xmldoc/).

The tags that form the core of the XML doc specification are:

// can't yet format InlineHtmlBlock ("<c>	<para>	<see>*	<value>
<code>	<param>*	<seealso>*	
<example>	<paramref>	<summary>	
<exception>*	<permission>*	<typeparam>*	
<include>*	<remarks>	<typeparamref>	
<list>	<inheritdoc>	<returns>	
", None, None) to pynb markdown

In addition, you may also use the [Recommended XML doc extensions for F# documentation tooling](https://github.com/fsharp/fslang-design/blob/master/tooling/FST-1031-xmldoc-extensions.md).

`<a href = "...">` links
Arbitrary paragraph-level HTML such as `<b>` for bold in XML doc text
`<namespacedoc>` giving documentation for the enclosing namespace
`<exclude>` to exclude from XML docs
`<category>` to give a category for an entity or member. An optional `index` attribute can be specified
to help sort the list of categories.
`\(...\)` for inline math and `$$...$$` and `\[...\]`for math environments, see [http://docs.mathjax.org.
Some](http://docs.mathjax.org.
Some) escaping of characters (e.g. `&lt;`, `&gt;`) may be needed to form valid XML
An example of an XML documentation comment, assuming the code is in namespace `TheNamespace`:

// can't yet format InlineHtmlBlock ("/// <summary>
///   A module
/// </summary>
///
/// <namespacedoc>
///   <summary>A namespace to remember</summary>
///
///   <remarks>More on that</remarks>
/// </namespacedoc>
///
module SomeModule = 
    /// <summary>
    ///   Some actual comment
    ///   <para>Another paragraph, see  <see cref="T:TheNamespace.SomeType"/>. </para>
    /// </summary>
    ///
    /// <param name="x">The input</param>
    ///
    /// <returns>The output</returns>
    ///
    /// <example>
    ///   Try using
    ///   <code>
    ///      open TheNamespace
    ///      SomeModule.a
    ///   </code>
    /// </example>
    ///
    /// <category>Foo</category>
   let someFunction x = 42 + x

/// <summary>
///   A type, see  <see cref="T:TheNamespace.SomeModule"/> and
///  <see cref="M:TheNamespace.SomeModule.someFunction"/>. 
/// </summary>
///
type SomeType() =
   member x.P = 1", None, None) to pynb markdown

Like types, members are referred to by xml doc sig.  These must currently be precise as the F#
compiler doesn't elaborate these references from simpler names:

// can't yet format InlineHtmlBlock ("type Class2() = 
    member this.Property = "more"
    member this.Method0() = "more"
    member this.Method1(c: string) = "more"
    member this.Method2(c: string, o: obj) = "more"

/// <see cref="P:TheNamespace.Class2.Property" />
/// and <see cref="M:TheNamespace.Class2.OtherMethod0" />
/// and <see cref="M:TheNamespace.Class2.Method1(System.String)" />
/// and <see cref="M:TheNamespace.Class2.Method2(System.String,System.Object)" />
let referringFunction1 () = "result"", None, None) to pynb markdown

Generic types are referred to by .NET compiled name, e.g.

// can't yet format InlineHtmlBlock ("type GenericClass2<'T>() = 
    member this.Property = "more"

    member this.NonGenericMethod(_c: 'T) = "more"

    member this.GenericMethod(_c: 'T, _o: 'U) = "more"

/// See <see cref="T:TheNamespace.GenericClass2`1" />
/// and <see cref="P:TheNamespace.GenericClass2`1.Property" />
/// and <see cref="M:TheNamespace.GenericClass2`1.NonGenericMethod(`0)" />
/// and <see cref="M:TheNamespace.GenericClass2`1.GenericMethod``1(`0,``0)" />
let referringFunction2 () = "result"

(*

## Go to Source links

'fsdocs' normally automatically adds GitHub links to each functions, values and class members for further reference.

This is normally done automatically based on the following settings:

    <RepositoryUrl>https://github.com/...</RepositoryUrl>
    <RepositoryBranch>...</RepositoryBranch>
    <RepositoryType>git</RepositoryType>

If your source is not built from the same project where you are building documentation then
you may need these settings:

    <FsDocsSourceRepository>...</FsDocsSourceRepository> -- the URL for the root of the source 
    <FsDocsSourceFolder>...</FsDocsSourceFolder>         -- the root soure folder at time of build

It is assumed that `sourceRepo` and `sourceFolder` have synchronized contents.

## Markdown Comments

You can use Markdown instead of XML in `///` comments. If you do, you should set `<UsesMarkdownComments>` in
your F# project file.

> Note: Markdown Comments are not supported in all F# IDE tooling.

### Adding cross-type links to modules and types in the same assembly

You can automatically add cross-type links to the documentation pages of other modules and types in the same assembly.
You can do this in two different ways:

* Add a [markdown inline link](https://github.com/adam-p/markdown-here/wiki/Markdown-Cheatsheet#links) were the link
title is the name of the type you want to link.

      /// this will generate a link to [Foo.Bar] documentation

* Add a [Markdown inline code](https://github.com/adam-p/markdown-here/wiki/Markdown-Cheatsheet#code) (using
back-ticks) where the code is the name of the type you want to link.

      /// This will also generate a link to `Foo.Bar` documentation

You can use either the full name (including namespace and module) or the simple name of a type.
If more than one type is found with the same name the link will not be generated.
If a type with the given name is not found in the same assembly the link will not be generated.
*)

/// Contains two types [Bar] and [Foo.Baz]
module Foo = 
    
    /// Bar is just an `int` and belongs to module [Foo]
    type Bar = int
    
    /// Baz contains a `Foo.Bar` as its `id`
    type Baz = { id: Bar }

    /// This function operates on `Baz` types.
    let f (b:Baz) = 
        b.id * 42

/// Referencing [Foo3] will not generate a link as there is no type with the name `Foo3`
module Foo3 =
    
    /// This is not the same type as `Foo.Bar`
    type Bar = double

    /// Using the simple name for [Bar] will fail to create a link because the name is duplicated in 
    /// [Foo.Bar] and in [Foo3.Bar]. In this case, using the full name works.
    let f2 b =
         b * 50", None, None) to pynb markdown

### Markdown Comments: Excluding APIs from the docs

If you want to exclude modules or functions from the API docs you can use the `[omit]` tag.
It needs to be set on a separate tripple-slashed line, but it could be either the first or the last:

// can't yet format InlineHtmlBlock ("/// [omit]
/// Some actual comment
module Bar = 
   let a = 42", None, None) to pynb markdown

## Building library documentation programmatically

You can build library documentation programatically using the functionality
in the [ApiDocs](https://fsprojects.github.io/FSharp.Formatting/reference/fsharp-formatting-apidocs-apidocs.html) type. To do this, load the assembly and open necessary namespaces:

// can't yet format InlineHtmlBlock ("#r "FSharp.Formatting.ApiDocs.dll"
open FSharp.Formatting.ApiDocs
open System.IO", None, None) to pynb markdown

For example the [ApiDocs.GenerateHtml](https://fsprojects.github.io/FSharp.Formatting/reference/fsharp-formatting-apidocs-apidocs.html) method:

// can't yet format InlineHtmlBlock ("let file = Path.Combine(root, "bin/YourLibrary.dll")
let input = ApiDocInput.FromFile(file) 
ApiDocs.GenerateHtml
    ( [ input ], 
      output=Path.Combine(root, "output"),
      collectionName="YourLibrary",
      template=Path.Combine(root, "templates", "template.html"),
      substitutions=[])", None, None) to pynb markdown


