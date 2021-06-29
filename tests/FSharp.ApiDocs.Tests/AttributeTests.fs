[<NUnit.Framework.TestFixture>]
module ApiDocs.AttributeTests

open FsUnit
open System.IO
open NUnit.Framework
open FSharp.Formatting.ApiDocs
open FSharp.Formatting.Templating
open FsUnitTyped
open NUnit.Framework.Internal
open System

// --------------------------------------------------------------------------------------
// Run the metadata formatter on sample project
// --------------------------------------------------------------------------------------

let (</>) a b = Path.Combine(a, b)
let fullpath = Path.GetFullPath
let fullpaths = List.map fullpath

let root = __SOURCE_DIRECTORY__ |> fullpath

let configuration =
#if DEBUG
    "Debug"
#else
    "Release"
#endif

let tfm = "netstandard2.1"

// NOTE - For these tests to run properly they require the output of all the metadata
// test project to be directed to the directory below
let testBin = __SOURCE_DIRECTORY__ </> "files/bin" </> configuration </> tfm |> fullpath

#if INTERACTIVE
;;
printfn "\n-- Root - %s" root;;
printfn "\n-- TestBin - %s" testBin;;
#endif

do FSharp.Formatting.TestHelpers.enableLogging()
let library = testBin </> "AttributesTestLib.dll"

let inputs = ApiDocInput.FromFile(library)

let findModule name (moduleInfos: ApiDocEntityInfo list)=
    moduleInfos
    |> List.filter (fun m-> not m.Entity.IsTypeDefinition)
    |> List.map (fun m-> m.Entity)
    |> List.find (fun m-> m.Name = name)

let findType name (typeInfos: ApiDocEntityInfo list)=
    typeInfos
    |> List.filter (fun m-> m.Entity.IsTypeDefinition)
    |> List.map (fun t -> t.Entity)
    |> List.find (fun t-> t.Name = name)

let info =
  [ ParamKeys.``fsdocs-collection-name``, "FSharp.ProjectScaffold"
    ParamKeys.``fsdocs-authors``, "Your Name"
    ParamKeys.``fsdocs-repository-link``, "http://github.com/pblasucci/fsharp-project-scaffold"
    ParamKeys.``root``, "http://fsprojects.github.io/FSharp.FSharp.ProjectScaffold" ]

[<Test>]
let ``ApiDocs extracts Attribute on Module``() =
  let modules = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let modul = modules |> findModule "SingleAttributeModule"
  let attribute = modul.Attributes.Head
  attribute.Name |> shouldEqual "ObsoleteAttribute"
  attribute.FullName |> shouldEqual "System.ObsoleteAttribute"
  attribute.ConstructorArguments |> shouldBeEmpty
  attribute.NamedConstructorArguments |> shouldBeEmpty

[<Test>]
let ``ApiDocs extracts multiple Attributes on Module``() =
  let modules = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let modul = modules |> findModule "MultipleAttributesModule"
  let attributes = modul.Attributes
  attributes.Length |> shouldEqual 3

[<Test>]
let ``ApiDocs extracts Attribute with argument``() =
  let modules = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let modul = modules |> findModule "SingleAttributeWithArgumentModule"
  let attribute = modul.Attributes.Head
  attribute.Name |> shouldEqual "ObsoleteAttribute"
  attribute.FullName |> shouldEqual "System.ObsoleteAttribute"
  attribute.ConstructorArguments |> shouldContain (box "obsolete")
  attribute.NamedConstructorArguments |> shouldBeEmpty


[<Test>]
let ``ApiDocs extracts Attribute with named arguments``() =
  let modules = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let modul = modules |> findModule "SingleAttributeWithNamedArgumentsModule"
  let attribute = modul.Attributes.Head
  attribute.Name |> shouldEqual "TestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.TestAttribute"
  attribute.ConstructorArguments |> shouldBeEmpty

  attribute.NamedConstructorArguments |>  shouldContain ("Int", box 1)
  attribute.NamedConstructorArguments |>  shouldContain ("String", box "test")
  let _,arrayArgument = attribute.NamedConstructorArguments |> Seq.find (fun (n,_)-> n="Array")
  arrayArgument |>  shouldEqual (box [| box "1"; box "2"|])
  ()

[<Test>]
let ``ApiDocs extracts Attribute on interface``() =
  let typeInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let interface' = typeInfos |> findType "AttributeInterface"
  let attribute = interface'.Attributes.Head
  attribute.Name |> shouldEqual "TestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.TestAttribute"

[<Test>]
let ``ApiDocs extracts Attribute on class``() =
  let typeInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let class' = typeInfos |> findType "AttributeClass"
  let attribute = class'.Attributes.Head
  attribute.Name |> shouldEqual "TestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.TestAttribute"

[<Test>]
let ``ApiDocs extracts Attribute on value in module``() =
  let typeInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let module' = typeInfos |> findModule "ContentTestModule"
  let testValue = module'.ValuesAndFuncs |> Seq.find (fun v -> v.Name ="testValue")
  let attribute = testValue.Attributes.Head
  attribute.Name |> shouldEqual "TestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.TestAttribute"

[<Test>]
let ``ApiDocs extracts Attribute on function in module``() =
  let typeInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let module' = typeInfos |> findModule "ContentTestModule"
  let testFunction = module'.ValuesAndFuncs |> Seq.find (fun v -> v.Name ="testFunction")
  let attribute = testFunction.Attributes.Head
  attribute.Name |> shouldEqual "TestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.TestAttribute"

[<Test>]
let ``ApiDocs extracts Attribute on instance member in class``() =
  let typeInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let class' = typeInfos |> findType "AttributeClass"
  let testMember = class'.InstanceMembers |> Seq.find (fun v -> v.Name ="TestMember")
  let attribute = testMember.Attributes.Head
  attribute.Name |> shouldEqual "TestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.TestAttribute"

[<Test>]
let ``ApiDocs extracts Attribute on static member in class``() =
  let typeInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let class' = typeInfos |> findType "AttributeClass"
  let staticMember = class'.StaticMembers |> Seq.find (fun v -> v.Name ="TestStaticMember")
  let attribute = staticMember.Attributes.Head
  attribute.Name |> shouldEqual "TestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.TestAttribute"

[<Test>]
let ``ApiDocs extracts Attribute on union case``() =
  let typeInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let union = typeInfos |> findType "AttributeUnion"
  let case = union.UnionCases |> Seq.find (fun v -> v.Name ="TestCase")
  let attribute = case.Attributes.Head
  attribute.Name |> shouldEqual "TestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.TestAttribute"
  attribute.NamedConstructorArguments |>  shouldContain ("String", box "union")

[<Test>]
let ``ApiDocs extracts Attribute on record field``() =
  let typeInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let union = typeInfos |> findType "AttributeRecord"
  let case = union.RecordFields |> Seq.find (fun v -> v.Name ="TestField")
  let attribute = case.Attributes.Head
  attribute.Name |> shouldEqual "TestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.TestAttribute"
  attribute.NamedConstructorArguments |>  shouldContain ("String", box "record")

[<Test>]
let ``ApiDocs formats attribute without arguments``() =
  let moduleInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let module' = moduleInfos |> findModule "FormatTestModule"
  let value = module'.ValuesAndFuncs |> Seq.find (fun v -> v.Name ="noArguments")
  let attribute = value.Attributes.Head
  attribute.Name |> shouldEqual "TestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.TestAttribute"
  attribute.Format() |> shouldEqual "[<Test>]"

[<Test>]
let ``ApiDocs formats attribute with single int argument``() =
  let moduleInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let module' = moduleInfos |> findModule "FormatTestModule"
  let value = module'.ValuesAndFuncs |> Seq.find (fun v -> v.Name ="singleIntArgument")
  let attribute = value.Attributes.Head
  attribute.Name |> shouldEqual "IntTestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.IntTestAttribute"
  attribute.Format() |> shouldEqual "[<IntTest(1)>]"

[<Test>]
let ``ApiDocs formats attribute with single string argument``() =
  let moduleInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let module' = moduleInfos |> findModule "FormatTestModule"
  let value = module'.ValuesAndFuncs |> Seq.find (fun v -> v.Name ="singleStringArgument")
  let attribute = value.Attributes.Head
  attribute.Name |> shouldEqual "StringTestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.StringTestAttribute"
  attribute.Format() |> shouldEqual """[<StringTest("test")>]"""

[<Test>]
let ``ApiDocs formats attribute with single bool argument``() =
  let moduleInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let module' = moduleInfos |> findModule "FormatTestModule"
  let value = module'.ValuesAndFuncs |> Seq.find (fun v -> v.Name ="singleBoolArgument")
  let attribute = value.Attributes.Head
  attribute.Name |> shouldEqual "BoolTestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.BoolTestAttribute"
  attribute.Format() |> shouldEqual "[<BoolTest(true)>]"


[<Test>]
let ``ApiDocs formats attribute with single array argument``() =
  let moduleInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let module' = moduleInfos |> findModule "FormatTestModule"
  let value = module'.ValuesAndFuncs |> Seq.find (fun v -> v.Name ="singleArrayArgument")
  let attribute = value.Attributes.Head
  attribute.Name |> shouldEqual "ArrayTestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.ArrayTestAttribute"
  attribute.Format() |> shouldEqual """[<ArrayTest([|"test1"; "test2"|])>]"""

[<Test>]
let ``ApiDocs formats attribute with multiple arguments``() =
  let moduleInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let module' = moduleInfos |> findModule "FormatTestModule"
  let value = module'.ValuesAndFuncs |> Seq.find (fun v -> v.Name ="multipleArguments")
  let attribute = value.Attributes.Head
  attribute.Name |> shouldEqual "MultipleTestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.MultipleTestAttribute"
  attribute.Format() |> shouldEqual """[<MultipleTest("test", 1, [|1; 2|])>]"""

[<Test>]
let ``ApiDocs formats attribute with multiple named arguments``() =
  let moduleInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let module' = moduleInfos |> findModule "FormatTestModule"
  let value = module'.ValuesAndFuncs |> Seq.find (fun v -> v.Name ="multipleNamedArguments")
  let attribute = value.Attributes.Head
  attribute.Name |> shouldEqual "TestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.TestAttribute"
  attribute.Format() |> shouldEqual """[<Test(Int = 1, String = "test", Array = [|"1"; "2"|])>]"""

[<Test>]
let ``ApiDocs formats attribute with name and suffix``() =
  let moduleInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let module' = moduleInfos |> findModule "FormatTestModule"
  let value = module'.ValuesAndFuncs |> Seq.find (fun v -> v.Name ="singleBoolArgument")
  let attribute = value.Attributes.Head
  attribute.Name |> shouldEqual "BoolTestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.BoolTestAttribute"
  attribute.FormatLongForm() |> shouldEqual "[<BoolTestAttribute(true)>]"
[<Test>]
let ``ApiDocs formats attribute with fullName``() =
  let moduleInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let module' = moduleInfos |> findModule "FormatTestModule"
  let value = module'.ValuesAndFuncs |> Seq.find (fun v -> v.Name ="singleBoolArgument")
  let attribute = value.Attributes.Head
  attribute.Name |> shouldEqual "BoolTestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.BoolTestAttribute"
  attribute.FormatFullName() |> shouldEqual "[<AttributeTestNamespace.BoolTest(true)>]"

[<Test>]
let ``ApiDocs formats attribute with fullName and suffix``() =
  let moduleInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let module' = moduleInfos |> findModule "FormatTestModule"
  let value = module'.ValuesAndFuncs |> Seq.find (fun v -> v.Name ="singleBoolArgument")
  let attribute = value.Attributes.Head
  attribute.Name |> shouldEqual "BoolTestAttribute"
  attribute.FullName |> shouldEqual "AttributeTestNamespace.BoolTestAttribute"
  attribute.FormatFullNameLongForm() |> shouldEqual "[<AttributeTestNamespace.BoolTestAttribute(true)>]"

[<Test>]
let ``ApiDocs IsObsolete returns true on obsolete attribute``() =
  let moduleInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let module' = moduleInfos |> findModule "ObsoleteTestModule"
  let noMessage = module'.ValuesAndFuncs |> Seq.find (fun v -> v.Name ="noMessage")
  noMessage.IsObsolete |> shouldEqual true
  noMessage.ObsoleteMessage |> shouldEqual ""

[<Test>]
let ``ApiDocs IsObsolete returns true on obsolete attribute and finds obsolete message``() =
  let moduleInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let module' = moduleInfos |> findModule "ObsoleteTestModule"
  let withMessage = module'.ValuesAndFuncs |> Seq.find (fun v -> v.Name ="withMessage")
  withMessage.IsObsolete |> shouldEqual true
  withMessage.ObsoleteMessage |> shouldEqual "obsolete"

[<Test>]
let ``ApiDocs IsObsolete returns false on not obsolete attribute and finds no obsolete message``() =
  let moduleInfos = ApiDocs.GenerateModel([inputs], collectionName="AttributeTestLib", substitutions=info, libDirs = [testBin]).EntityInfos
  let module' = moduleInfos |> findModule "ObsoleteTestModule"
  let notObsolete = module'.ValuesAndFuncs |> Seq.find (fun v -> v.Name ="notObsolete")
  notObsolete.IsObsolete |> shouldEqual false
  notObsolete.ObsoleteMessage |> shouldEqual ""
