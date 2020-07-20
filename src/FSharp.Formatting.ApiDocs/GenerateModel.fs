namespace FSharp.Formatting.ApiDocs

open System
open System.Reflection
open System.Collections.Generic
open System.Text
open System.IO
open System.Xml
open System.Xml.Linq

open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.Range
open FSharp.Formatting.Common
open FSharp.Formatting.Internal
open FSharp.Patterns
open FSharp.Formatting.CodeFormat

/// Represents a documentation comment attached to source code
type ApiDocComment(blurb, full, sects, rawData) =
    /// The summary for the comment
  member x.Blurb : string = blurb

    /// The full text of the comment
  member x.FullText : string = full

    /// The parsed sections of the comment
  member x.Sections : KeyValuePair<string, string> list = sects

    /// The raw data of the comment
  member x.RawData: KeyValuePair<string, string> list = rawData

  static member internal Empty = ApiDocComment("", "", [], [])

/// Represents a custom attribute attached to source code
type ApiDocAttribute(name, fullName, constructorArguments, namedConstructorArguments) =
    /// The name of the attribute
  member x.Name : string = name

    /// The qualified name of the attribute
  member x.FullName : string = fullName

    /// The arguments to the constructor for the attribute
  member x.ConstructorArguments : obj list = constructorArguments

    /// The named arguments for the attribute
  member x.NamedConstructorArguments : (string*obj) list = namedConstructorArguments

  /// Gets a value indicating whether this attribute is System.ObsoleteAttribute
  member x.IsObsoleteAttribute =
    x.FullName = "System.ObsoleteAttribute"

  /// Gets a value indicating whether this attribute is RequireQualifiedAccessAttribute
  member x.IsRequireQualifiedAccessAttribute =
    x.FullName = typeof<RequireQualifiedAccessAttribute>.FullName

  /// Returns the obsolete message, when this attribute is the System.ObsoleteAttribute. When its not or no message was specified, an empty string is returned
  member x.ObsoleteMessage =
    let tryFindObsoleteMessage =
            x.ConstructorArguments
            |> Seq.tryFind (fun x -> x :? string)
            |> Option.map string
            |> Option.defaultValue ""
    if x.IsObsoleteAttribute then tryFindObsoleteMessage else ""

  /// Gets a value indicating whether this attribute the CustomOperationAttribute
  member x.IsCustomOperationAttribute =
    x.FullName = "Microsoft.FSharp.Core.CustomOperationAttribute"

  /// Returns the custom operation name, when this attribute is the CustomOperationAttribute. When its not an empty string is returned
  member x.CustomOperationName =
    let tryFindCustomOperation =
      x.ConstructorArguments
      |> Seq.tryFind (fun x -> x :? string)
      |> Option.map string
      |> Option.defaultValue ""
    if x.IsCustomOperationAttribute then tryFindCustomOperation else ""

  /// Formats the attribute with the given name
  member private x.Format(attributeName:string, removeAttributeSuffix:bool) =
        let dropSuffix (s:string) (t:string) = s.[0..s.Length - t.Length - 1]
        let attributeName = if removeAttributeSuffix && attributeName.EndsWith "Attribute" then dropSuffix attributeName "Attribute" else attributeName
        let join sep (items : string seq) = String.Join(sep, items)
        let inline append (s:string) (sb:StringBuilder) = sb.Append(s)
        let inline appendIfTrue p s sb =
            if p then append s sb
                else sb

        let rec formatValue (v:obj) =
            match v with
            | :? string as s -> sprintf "\"%s\"" s
            | :? array<obj> as a -> a |> Seq.map formatValue |> join "; " |> sprintf "[|%s|]"
            | :? bool as b -> if b then "true" else "false"
            | _ -> string v
        let formatedConstructorArguments =
            x.ConstructorArguments
            |> Seq.map formatValue
            |> join ", "

        let formatedNamedConstructorArguments =
            x.NamedConstructorArguments
            |> Seq.map (fun (n,v) -> sprintf "%s = %s" n (formatValue v))
            |> join ", "
        let needsBraces = not (List.isEmpty x.ConstructorArguments && List.isEmpty x.NamedConstructorArguments)
        let needsListSeperator = not (List.isEmpty x.ConstructorArguments || List.isEmpty x.NamedConstructorArguments)
        StringBuilder()
        |> append "[<"
        |> append attributeName
        |> appendIfTrue needsBraces "("
        |> append formatedConstructorArguments
        |> appendIfTrue needsListSeperator ", "
        |> append formatedNamedConstructorArguments
        |> appendIfTrue needsBraces ")"
        |> append ">]"
        |> string

  /// Formats the attribute using the Name. Removes the "Attribute"-suffix. E.g Obsolete
  member x.Format() = x.Format(x.Name, true)

  /// Formats the attribute using the FullName. Removes the "Attribute"-suffix. E.g System.Obsolete
  member x.FormatFullName() = x.Format(x.FullName, true)

  /// Formats the attribute using the Name. Keeps the "Attribute"-suffix. E.g ObsoleteAttribute
  member x.FormatLongForm() = x.Format(x.Name, false)

  /// Formats the attribute using the FullName. Keeps the "Attribute"-suffix. E.g System.ObsoleteAttribute
  member x.FormatFullNameLongForm() = x.Format(x.FullName, false)

  /// Tries to find the System.ObsoleteAttribute and return its obsolete message
  static member internal TryGetObsoleteMessage(attributes:seq<ApiDocAttribute>)=
    attributes
    |> Seq.tryFind (fun a -> a.IsObsoleteAttribute)
    |> Option.map (fun a -> a.ObsoleteMessage)
    |> Option.defaultValue ""

  /// Tries to find the CustomOperationAttribute and return its obsolete message
  static member internal TryGetCustomOperationName(attributes:seq<ApiDocAttribute>)=
    attributes
    |> Seq.tryFind (fun a -> a.IsCustomOperationAttribute)
    |> Option.map (fun a -> a.CustomOperationName)
    |> Option.defaultValue ""


/// Represents the kind of member
type ApiDocMemberKind =
  // In a module
  | ValueOrFunction = 0
  | TypeExtension = 1
  | ActivePattern = 2

  // In a class
  | Constructor = 3
  | InstanceMember = 4
  | StaticMember = 5

  // In a class, F# special members
  | UnionCase = 100
  | RecordField = 101
  | StaticParameter = 102

/// Represents an method, property, constructor, function or value, record field, union case or static parameter
/// integrated with its associated documentation. Includes extension members.
type ApiDocMember(displayName: string, attributes: ApiDocAttribute list, entityUrl, kind, cat, details, comment, symbol) =
  // The URL for a member is currently the #DisplayName on the enclosing entity
  let url = sprintf "%s.html#%s" entityUrl displayName
  let (usage, usageTooltip, parameterTooltips, returnTooltip, mods, typars, signatureTooltip, location, compiledName) = details

    /// The member's modifiers
  member x.Modifiers : string list = mods

    /// The member's type arguments
  member x.TypeArguments : string list = typars

    /// The usage section in a typical tooltip
  member x.UsageTooltip : string = usageTooltip

    /// The return section in a typical tooltip
  member x.ReturnTooltip : string option = returnTooltip

    /// The full signature section in a typical tooltip
  member x.SignatureTooltip : string = signatureTooltip

    /// The member's parameter sections in a typical tooltip
  member x.ParameterTooltips: (string * string) list = parameterTooltips

    /// The member's source location, if any
  member x.SourceLocation : string option = location

/// The member's compiled name, if any
  [<Obsolete("Remove '.Details'. 'ApiDocsMemberDetails' has been merged with 'ApiDocsMember'", true)>]
  member x.Details : int = 0

    /// The member's compiled name, if any
  member x.CompiledName : string option = compiledName

  /// Formats usage
  member x.FormatUsage(maxLength) = usage(maxLength)

  /// Formats type arguments
  member x.FormatTypeArguments = String.concat ", " x.TypeArguments

  /// Formats modifiers
  member x.FormatModifiers = String.concat " " x.Modifiers

  /// Formats source location
  member x.FormatSourceLocation = defaultArg x.SourceLocation ""

  /// Formats the compiled name
  member x.FormatCompiledName = defaultArg x.CompiledName ""

    /// Name of the member
  member x.Name = displayName

    /// The URL of the best link documentation for the item (without the http://site.io/reference)
  member x.UrlBaseName = entityUrl

    /// The URL of the best link documentation for the item (without the http://site.io/reference)
  member x.UrlFileNameAndHash = url

    /// The declared attributes of the member
  member x.Attributes = attributes

    /// The category
  member x.Category : string = cat

    /// The kind of the member
  member x.Kind : ApiDocMemberKind = kind

    /// The attached comment
  member x.Comment : ApiDocComment = comment

    /// The symbol this member is related to
  member x.Symbol : FSharpSymbol = symbol

  /// Gets a value indicating whether this member is obsolete
  member x.IsObsolete =
    x.Attributes
    |> Seq.exists (fun a -> a.IsObsoleteAttribute)

  /// Returns the obsolete message, when this member is obsolete. When its not or no message was specified, an empty string is returned
  member x.ObsoleteMessage =
    ApiDocAttribute.TryGetObsoleteMessage(x.Attributes)

  member x.IsRequireQualifiedAccessAttribute =
    x.Attributes
    |> Seq.exists (fun a -> a.IsRequireQualifiedAccessAttribute)

  /// Gets a value indicating whether this member has CustomOperationAttribute
  member x.IsCustomOperation =
    x.Attributes
    |> Seq.exists (fun a -> a.IsCustomOperationAttribute)

  /// Returns the custom operation name, when this attribute is the CustomOperationAttribute. When its not an empty string is returned
  member x.CustomOperationName =
    ApiDocAttribute.TryGetCustomOperationName(x.Attributes)

/// Represents a type definition integrated with its associated documentation
type ApiDocType(name, cat, url, comment, assembly, attributes, cases, fields, statParams, ctors, inst, stat) =
    /// The name of the type
  member x.Name : string = name

    /// The category of the type
  member x.Category :string = cat

    /// The URL of the best link documentation for the item (without the http://site.io/reference or .html)
  member x.UrlBaseName : string = url

    /// The attached comment
  member x.Comment : ApiDocComment = comment

    /// The name of the type's assembly
  member x.Assembly : AssemblyName = assembly

    /// The declared attributes of the type
  member x.Attributes : ApiDocAttribute list = attributes

    /// The cases of a union type
  member x.UnionCases : ApiDocMember list = cases

    /// The fields of a record type
  member x.RecordFields : ApiDocMember list = fields

    /// Static parameters
  member x.StaticParameters : ApiDocMember list = statParams

    /// All members of the type
  member x.AllMembers : ApiDocMember list = List.concat [ ctors; inst; stat; cases; fields; statParams ]

    /// The constuctorsof the type
  member x.Constructors : ApiDocMember list = ctors

    /// The instance members of the type
  member x.InstanceMembers : ApiDocMember list = inst

    /// The static members of the type
  member x.StaticMembers : ApiDocMember list = stat

  /// Gets a value indicating whether this member is obsolete
  member x.IsObsolete =
    x.Attributes
    |> Seq.exists (fun a -> a.IsObsoleteAttribute)

  /// Returns the obsolete message, when this member is obsolete. When its not or no message was specified, an empty string is returned
  member x.ObsoleteMessage =
    ApiDocAttribute.TryGetObsoleteMessage(x.Attributes)

/// Represents an F# module definition integrated with its associated documentation
type ApiDocModule(name, cat, url, comment, assembly, attributes, modules, types, vals, exts, pats) =
    /// The name of the module
  member x.Name : string = name

    /// The category of the module
  member x.Category : string = cat

    /// The URL of the best link documentation for the item (without the http://site.io/reference or .html)
  member x.UrlBaseName : string = url

    /// The attached comment
  member x.Comment : ApiDocComment = comment

    /// The name of the module's assembly
  member x.Assembly : AssemblyName = assembly

    /// The declared attributes of the module
  member x.Attributes : ApiDocAttribute list = attributes

    /// All members of the module
  member x.AllMembers : ApiDocMember list = List.concat [ vals; exts; pats ]

    /// All nested modules
  member x.NestedModules : ApiDocModule list = modules

    /// All nested types
  member x.NestedTypes : ApiDocType list = types

    /// Values and functions of the module
  member x.ValuesAndFuncs : ApiDocMember list = vals

    /// Type extensions of the module
  member x.TypeExtensions : ApiDocMember list = exts

    /// Active patterns of the module
  member x.ActivePatterns : ApiDocMember list = pats

  /// Gets a value indicating whether this member is obsolete
  member x.IsObsolete =
    x.Attributes
    |> Seq.exists (fun a -> a.IsObsoleteAttribute)

  /// Returns the obsolete message, when this member is obsolete. When its not or no message was specified, an empty string is returned
  member x.ObsoleteMessage =
    ApiDocAttribute.TryGetObsoleteMessage(x.Attributes)

/// Represents a namespace integrated with its associated documentation
type ApiDocNamespace(name, mods, typs) =
    /// The name of the namespace
  member x.Name : string = name

    /// All modules in the namespace
  member x.Modules : ApiDocModule list = mods

    /// All types in the namespace
  member x.Types : ApiDocType list = typs

/// Represents a group of assemblies integrated with its associated documentation
type ApiDocAssemblyGroup(name: string, asms: AssemblyName list, nss: ApiDocNamespace list) =
    /// Name of the group
  member x.Name = name

    /// All assemblies in the group
  member x.Assemblies = asms

    /// All namespaces in the group
  member x.Namespaces = nss

/// High-level information about a module definition
type ApiDocModuleInfo(modul: ApiDocModule, asm: ApiDocAssemblyGroup, ns: ApiDocNamespace, parent: ApiDocModule option) =
    /// The actual module
  member x.Module = modul

    /// The assembly group the module belongs to
  member x.Assembly = asm

    /// The namespace the module belongs to
  member x.Namespace = ns

    /// The parent module, if any.
  member x.ParentModule = parent

  member this.HasParentModule = this.ParentModule.IsSome


/// High-level information about a type definition
type ApiDocTypeInfo(typ, asm, ns, modul) =
    /// The actual type
  member x.Type : ApiDocType = typ

    /// The assembly group the type belongs to
  member x.Assembly : ApiDocAssemblyGroup = asm

    /// The namespace the type belongs to
  member x.Namespace : ApiDocNamespace = ns

    /// The parent module, if any.
  member x.ParentModule : ApiDocModule option = modul

  member this.HasParentModule = this.ParentModule.IsSome


module internal ValueReader =
  type CrefReference =
    { IsInternal : bool; ReferenceLink : string; NiceName : string }

  type IUrlHolder =
    abstract RegisterEntity : FSharpEntity -> unit
    abstract GetUrl : FSharpEntity -> string
    abstract ResolveCref : string -> CrefReference option

  type ReadingContext =
    { PublicOnly : bool
      Assembly : AssemblyName
      XmlMemberMap : IDictionary<string, XElement>
      UrlMap : IUrlHolder
      MarkdownComments : bool
      UrlRangeHighlight : Uri -> int -> int -> string
      SourceFolderRepository : (string * string) option
      AssemblyPath : string
      CompilerOptions : string
      FormatAgent : CodeFormatAgent }

    member x.XmlMemberLookup(key) =
      match x.XmlMemberMap.TryGetValue(key) with
      | true, v -> Some v
      | _ -> None

    static member internal Create
        (publicOnly, assembly, map, sourceFolderRepo, urlRangeHighlight, markDownComments, urlMap,
         assemblyPath, compilerOptions, formatAgent ) =

      { PublicOnly=publicOnly
        Assembly = assembly
        XmlMemberMap = map
        MarkdownComments = markDownComments
        UrlMap = urlMap
        UrlRangeHighlight = urlRangeHighlight
        SourceFolderRepository = sourceFolderRepo
        AssemblyPath = assemblyPath
        CompilerOptions = compilerOptions
        FormatAgent = formatAgent }

  let inline private getCompiledName (s : ^a when ^a :> FSharpSymbol) =
    let compiledName = (^a : (member CompiledName : string) (s))
    match compiledName = s.DisplayName with
    | true -> None
    | _    -> Some compiledName

  let formatSourceLocation (urlRangeHighlight : Uri -> int -> int -> string) (sourceFolderRepo : (string * string) option) (location : range option) =
    location |> Option.bind (fun location ->
        sourceFolderRepo |> Option.map (fun (baseFolder, repo) ->
            let basePath = Uri(Path.GetFullPath(baseFolder)).ToString()
            let docPath = Uri(Path.GetFullPath(location.FileName)).ToString()

            // Even though ignoring case might be wrong, we do that because
            // one path might be file:///C:\... and the other file:///c:\...  :-(
            if not <| docPath.StartsWith(basePath, StringComparison.InvariantCultureIgnoreCase) then
                Log.errorf "Current source file '%s' doesn't reside in source folder '%s'" docPath basePath
                ""
            else
                let relativePath = docPath.[basePath.Length..]
                let uriBuilder = UriBuilder(repo)
                uriBuilder.Path <- uriBuilder.Path + relativePath
                urlRangeHighlight uriBuilder.Uri location.StartLine location.EndLine ) )

  let (|AllAndLast|_|) (list:'T list)=
    if list.IsEmpty then None
    else let revd = List.rev list in Some(List.rev revd.Tail, revd.Head)

  let isAttrib<'T> (attrib: FSharpAttribute) =
    attrib.AttributeType.CompiledName = typeof<'T>.Name

  let hasAttrib<'T> (attribs: IList<FSharpAttribute>) =
    attribs |> Seq.exists (fun a -> isAttrib<'T>(a))

  let (|MeasureProd|_|) (typ : FSharpType) =
      if typ.HasTypeDefinition && typ.TypeDefinition.LogicalName = "*" && typ.GenericArguments.Count = 2 then Some (typ.GenericArguments.[0], typ.GenericArguments.[1])
      else None

  let (|MeasureInv|_|) (typ : FSharpType) =
      if typ.HasTypeDefinition && typ.TypeDefinition.LogicalName = "/" && typ.GenericArguments.Count = 1 then Some typ.GenericArguments.[0]
      else None

  let (|MeasureOne|_|) (typ : FSharpType) =
      if typ.HasTypeDefinition && typ.TypeDefinition.LogicalName = "1" && typ.GenericArguments.Count = 0 then  Some ()
      else None

  let formatTypeArgument (typar:FSharpGenericParameter) =
    (if typar.IsSolveAtCompileTime then "^" else "'") + typar.Name

  let formatTypeArguments (typars:seq<FSharpGenericParameter>) =
    Seq.map formatTypeArgument typars |> List.ofSeq

  let bracket (str:string) =
    if str.Contains(" ") then "(" + str + ")" else str

  let bracketIf cond str =
    if cond then "(" + str + ")" else str

  let formatTyconRef (tcref:FSharpEntity) =
    // TODO: layoutTyconRef generates hyperlinks
    tcref.DisplayName

  let rec formatTypeApplication typeName prec prefix args =
    if prefix then
      match args with
      | [] -> typeName
      | [arg] -> typeName + "<" + (formatTypeWithPrec 4 arg) + ">"
      | args -> bracketIf (prec <= 1) (typeName + "<" + (formatTypesWithPrec 2 ", " args) + ">")
    else
      match args with
      | [] -> typeName
      | [arg] -> (formatTypeWithPrec 2 arg) + " " + typeName
      | args -> bracketIf (prec <= 1) ((bracket (formatTypesWithPrec 2 ", " args)) + typeName)

  and formatTypesWithPrec prec sep typs =
    String.concat sep (typs |> Seq.map (formatTypeWithPrec prec))

  and formatTypeWithPrec prec (typ:FSharpType) =
    // Measure types are stored as named types with 'fake' constructors for products, "1" and inverses
    // of measures in a normalized form (see Andrew Kennedy technical reports). Here we detect this
    // embedding and use an approximate set of rules for layout out normalized measures in a nice way.
    match typ with
    | MeasureProd (ty,MeasureOne)
    | MeasureProd (MeasureOne, ty) -> formatTypeWithPrec prec ty
    | MeasureProd (ty1, MeasureInv ty2)
    | MeasureProd (ty1, MeasureProd (MeasureInv ty2, MeasureOne)) ->
        (formatTypeWithPrec 2 ty1) + "/" + (formatTypeWithPrec 2 ty2)
    | MeasureProd (ty1,MeasureProd(ty2,MeasureOne))
    | MeasureProd (ty1,ty2) ->
        (formatTypeWithPrec 2 ty1) + "*" + (formatTypeWithPrec 2 ty2)
    | MeasureInv ty -> "/" + (formatTypeWithPrec 1 ty)
    | MeasureOne  -> "1"
    | _ when typ.HasTypeDefinition ->
        let tcref = typ.TypeDefinition
        let tyargs = typ.GenericArguments |> Seq.toList
        // layout postfix array types
        formatTypeApplication (formatTyconRef tcref) prec tcref.UsesPrefixDisplay tyargs
    | _ when typ.IsTupleType ->
        let tyargs = typ.GenericArguments |> Seq.toList
        bracketIf (prec <= 2) (formatTypesWithPrec 2 " * " tyargs)
    | _ when typ.IsFunctionType ->
        let rec loop soFar (typ:FSharpType) =
          if typ.IsFunctionType then
            let domainTyp, retType = typ.GenericArguments.[0], typ.GenericArguments.[1]
            loop (soFar + (formatTypeWithPrec 4 typ.GenericArguments.[0]) + " -> ") retType
          else
            soFar + formatTypeWithPrec 5 typ
        bracketIf (prec <= 4) (loop "" typ)
    | _ when typ.IsGenericParameter ->
        formatTypeArgument typ.GenericParameter
    | _ -> "(type)"

  let formatType typ =
    formatTypeWithPrec 5 typ

  let isUnitType (ty: FSharpType) =
      ty.HasTypeDefinition && ty.TypeDefinition.XmlDocSig = "T:Microsoft.FSharp.Core.unit"

  let formatArgNameAndType i (arg:FSharpParameter) =
    let nm =
      match arg.Name with
      | None ->
          if isUnitType arg.Type then "()"
          else "arg" + string i
      | Some nm -> nm
    let isOptionalArg = arg.IsOptionalArg || hasAttrib<OptionalArgumentAttribute> arg.Attributes
    let argName = if isOptionalArg then "?" + nm else nm
    let argType = arg.Type
    let argType =
        // Strip off the 'option' type for optional arguments
        if isOptionalArg && argType.HasTypeDefinition && argType.GenericArguments.Count = 1 then
            argType.GenericArguments.[0]
        else
            argType
    argName, argType

  let internal (++) (a:string) (b:string) =
    match String.IsNullOrEmpty a, String.IsNullOrEmpty b with
    | true, true -> ""
    | false, true -> a
    | true, false -> b
    | false, false -> a + " " + b

  let formatParameter (p:FSharpParameter) =
    try formatType p.Type
    with :? InvalidOperationException -> p.DisplayName


  //Formats argument list for "signature" information with padding
  let formatSignature (v:FSharpMemberOrFunctionOrValue) (args: list<list<FSharpParameter>>) =

    let ident = 3
    let maxPadding = 20
    let indent = String.replicate ident " "

    let maxUnderThreshold nmax =
        List.maxBy(fun n -> if n > nmax then 0 else n)

    let padLength =
      let allLengths =
        args
        |> List.concat
        |> List.map (fun p -> let name = Option.defaultValue p.DisplayName p.Name
                              let normalisedName = PrettyNaming.QuoteIdentifierIfNeeded name
                              normalisedName.Length )
      match allLengths with
      | [] -> 0
      | l -> l |> maxUnderThreshold maxPadding

    let allParamsLengths =
      args |> List.map (List.map (fun p -> (formatParameter p).Length) >> List.sum)
    let maxLength = (allParamsLengths |> maxUnderThreshold maxPadding) + 1

    let parameterTypeWithPadding (p: FSharpParameter) length =
      (formatParameter p) + (String.replicate (if length >= maxLength then 1 else maxLength - length) " ")

    let formatName indent padding (parameter:FSharpParameter) =
      let name = Option.defaultValue parameter.DisplayName parameter.Name
      let normalisedName = PrettyNaming.QuoteIdentifierIfNeeded name
      indent + name.PadRight padding + ":"

    let formatParameterPadded length p =
      formatName indent padLength p ++ (parameterTypeWithPadding p length)

    let allParams =
        List.zip args allParamsLengths
        |> List.map(fun (paramTypes, length) ->
                        paramTypes
                        |> List.map (formatParameterPadded length)
                        |> String.concat (" *\n"))
        |> String.concat ("->\n")

    allParams +  "\n" + indent + (String.replicate (max (padLength-1) 0) " ") + "->"

  //Formats argument list for "signature" information without padding
  let formatSignatureNoPadding (v:FSharpMemberOrFunctionOrValue) (args: list<list<FSharpParameter>>) =
    let isItemIndexer = (v.IsInstanceMember && v.DisplayName = "Item")
    let counter = let n = ref 0 in fun () -> incr n; !n
    let unit, argSep, tupSep = "unit", " -> ", " * "
    args
    |> List.map (List.map (fun x -> formatParameter x))
    |> List.map (function
        | [] -> unit
        | [arg] when arg = unit -> unit
        | [arg] when not v.IsMember || isItemIndexer -> arg
        | args when isItemIndexer -> String.concat tupSep args
        | args -> bracket (String.concat tupSep args))
    |> String.concat argSep

  // Format each argument, including its name and type for "usage"
  let formatArgUsage i (arg:FSharpParameter) =
    let argName, argType = formatArgNameAndType i arg
    argName

  //Formats argument list for "usage"
  let formatArgsUsage (v:FSharpMemberOrFunctionOrValue) args =
    let isItemIndexer = (v.IsInstanceMember && v.DisplayName = "Item")
    let counter = let n = ref 0 in fun () -> incr n; !n
    let unit, argSep, tupSep = "()", " ", ", "
    args
    |> List.map (List.map (fun x -> formatArgUsage (counter()) x))
    |> List.map (function
        | [] -> unit
        | [arg] when arg = unit -> unit
        | [arg] when not v.IsMember || isItemIndexer -> arg
        | args when isItemIndexer -> String.concat tupSep args
        | args -> bracket (String.concat tupSep args))
    |> String.concat argSep

  let tryGetLocation (symbol: FSharpSymbol) =
    match symbol.ImplementationLocation with
    | Some loc -> Some loc
    | None -> symbol.DeclarationLocation

  let readAttribute (attribute: FSharpAttribute) =
    let name = attribute.AttributeType.DisplayName
    let fullName = attribute.AttributeType.FullName
    let constructorArguments = attribute.ConstructorArguments |> Seq.map snd |> Seq.toList
    let namedArguments = attribute.NamedArguments |> Seq.map (fun (_,name,_,value) -> (name, value))  |> Seq.toList
    ApiDocAttribute(name, fullName, constructorArguments, namedArguments)

  let readAttributes (attributes:seq<FSharpAttribute>) =
    attributes
    |> Seq.map readAttribute
    |> Seq.toList

  let readMemberOrVal (ctx:ReadingContext) (v:FSharpMemberOrFunctionOrValue) =
    // we calculate this early just in case this fails with an FCS error.
    let requireQualifiedAccess =
        hasAttrib<RequireQualifiedAccessAttribute> v.ApparentEnclosingEntity.Attributes

    let argInfos = v.CurriedParameterGroups |> Seq.map Seq.toList |> Seq.toList

    let buildUsage (args:string option) =

      let parArgs =
        args |> Option.map (fun s ->
          if String.IsNullOrWhiteSpace(s) then ""
          elif s.StartsWith("(") then s
          // curried arguments should not have brackets, see https://github.com/fsprojects/FSharp.Formatting/issues/472
          elif argInfos.Length > 1 then " " + s
          else sprintf "(%s)" s)

      match v.IsMember, v.IsInstanceMember, v.LogicalName, v.DisplayName with
      // Constructors
      | _, _, ".ctor", _ -> v.ApparentEnclosingEntity.DisplayName + (defaultArg parArgs "(...)")
      // Indexers
      | _, true, _, "Item" -> "this.[" + (defaultArg args "...") + "]"
      // Ordinary instance members
      | _, true, _, name -> "this." + name + (defaultArg parArgs "(...)")
      // Ordinary functions or values
      | false, _, _, name when not requireQualifiedAccess -> name + (defaultArg parArgs "(...)")
      // Ordinary static members or things (?) that require fully qualified access
      | _, false, _, name -> v.ApparentEnclosingEntity.DisplayName + "." + name + (defaultArg parArgs "(...)")

    let modifiers =
      [ // TODO: v.Accessibility does not contain anything
        if v.InlineAnnotation = FSharpInlineAnnotation.AlwaysInline then yield "inline"
        if v.IsDispatchSlot then yield "abstract" ]

    let retType = v.ReturnParameter.Type
    let argInfos, retType =
        match argInfos, v.IsPropertyGetterMethod || v.HasGetterMethod, v.IsPropertySetterMethod || v.HasSetterMethod with
        | [ AllAndLast(args, last) ], _, true -> [ args ], Some last.Type
        | _, _, true -> argInfos, None
        | [[]], true, _ -> [], Some retType
        | _, _, _ -> argInfos, Some retType

    let paramTooltips =
        argInfos
        |> List.concat
        |> List.mapi (fun i p -> let nm, ty = formatArgNameAndType i p in nm, formatType ty)

    // Extension members can have apparent parents which are not F# types.
    // Hence getting the generic argument count if this is a little trickier
    let numGenericParamsOfApparentParent =
        let pty = v.ApparentEnclosingEntity
        //if pty.IsExternal then
        //    let ty = v.LogicalEnclosingEntity.ReflectionType
        //    if ty.IsGenericType then ty.GetGenericArguments().Length
        //    else 0
        //else
        pty.GenericParameters.Count

    // Ensure that there is enough number of elements to skip
    let tps = v.GenericParameters |> Seq.skip (min v.GenericParameters.Count numGenericParamsOfApparentParent)
    let typars = formatTypeArguments tps

    //let cxs  = indexedConstraints v.GenericParameters
    let retTypeText = defaultArg (retType |> Option.map formatType) "unit"

    let returnTooltip =
       match retType with None -> None | Some retType -> if isUnitType retType then None else Some retTypeText

    let signatureTooltip =
      match argInfos with
      | [] -> retTypeText
      | [[x]] when (v.IsPropertyGetterMethod || v.HasGetterMethod) && x.Name.IsNone && isUnitType x.Type -> retTypeText
      | _  -> (formatSignature v argInfos) + " " + retTypeText

    let fullArgUsage =
      match argInfos with
      | [[x]] when (v.IsPropertyGetterMethod || v.HasGetterMethod) && x.Name.IsNone && isUnitType x.Type -> ""
      | _  -> formatArgsUsage v argInfos

    let usageTooltip = buildUsage (Some fullArgUsage)

    let buildShortUsage length =
      if usageTooltip.Length <= length then usageTooltip
      else buildUsage None

    // If there is a signature file, we should go for implementation file
    let loc = tryGetLocation v
    let location = formatSourceLocation ctx.UrlRangeHighlight ctx.SourceFolderRepository loc
    (buildShortUsage, usageTooltip, paramTooltips, returnTooltip, modifiers, typars, signatureTooltip, location, getCompiledName v)

  let readUnionCase (ctx:ReadingContext) (typ: FSharpEntity) (case:FSharpUnionCase) =

    let requireQualifiedAccess =
        hasAttrib<RequireQualifiedAccessAttribute> typ.Attributes

    let formatFieldUsage (field:FSharpField) =
        if field.Name.StartsWith("Item") then
            formatType field.FieldType
        else
            field.Name

    let fields = case.UnionCaseFields |> List.ofSeq

    let buildUsage maxLength =
        let long = "(" + (fields |> List.map formatFieldUsage |> String.concat ", ") + ")"
        match long.Length with
        | x when x <= 2 -> ""
        | x when x <= maxLength -> long
        | _ -> "(...)"

    let paramTooltips =
        fields |> List.map (fun p -> p.Name, formatType p.FieldType)

    let returnTooltip = None
       //if isUnitType retType then None else Some retTypeText

    let usage (maxLength:int) =
       let nm = (if requireQualifiedAccess then typ.DisplayName + "." else "") + case.Name
       nm + buildUsage (maxLength - nm.Length)

    let usageTooltip = usage Int32.MaxValue
    let modifiers = List.empty
    let typeparams = List.empty

    let retTypeText = formatType case.ReturnType
    let signatureTooltip =
       match fields with
       | [] -> retTypeText
       | _ -> (fields |> List.map (fun field -> formatType field.FieldType) |> String.concat " * ") + " -> " + retTypeText
    let loc = tryGetLocation case
    let location = formatSourceLocation ctx.UrlRangeHighlight ctx.SourceFolderRepository loc
    (usage, usageTooltip, paramTooltips, returnTooltip, modifiers, typeparams, signatureTooltip, location, getCompiledName case)

  let readFSharpField (ctx:ReadingContext) (field:FSharpField) =
    let usage (maxLength:int) = field.Name
    let modifiers =
      [ if field.IsMutable then yield "mutable"
        if field.IsStatic then yield "static" ]
    let typeparams = List.empty
    let signatureTooltip = formatType field.FieldType
    let paramTooltips = []

    let returnTooltip = None
       //if isUnitType retType then None else Some retTypeText
    let loc = tryGetLocation field
    let location = formatSourceLocation ctx.UrlRangeHighlight ctx.SourceFolderRepository loc
    let usageTooltip = usage Int32.MaxValue
    (usage, usageTooltip, paramTooltips, returnTooltip, modifiers, typeparams, signatureTooltip, location, if field.Name <> field.DisplayName then Some field.Name else None)

  let getFSharpStaticParamXmlSig (typeProvider:FSharpEntity) parameterName =
    "SP:" + typeProvider.AccessPath + "." + typeProvider.LogicalName + "." + parameterName

  let readFSharpStaticParam (ctx:ReadingContext) (staticParam:FSharpStaticParameter) =
    let usage (maxLength:int) = staticParam.Name
    let modifiers = List.empty
    let typeparams = List.empty
    let paramTooltips = []
    let returnTooltip = None
    let signatureTooltip = formatType staticParam.Kind + (if staticParam.IsOptional then sprintf " (optional, default = %A)" staticParam.DefaultValue else "")
    let loc = tryGetLocation staticParam
    let location = formatSourceLocation ctx.UrlRangeHighlight ctx.SourceFolderRepository loc
    let usageTooltip = usage Int32.MaxValue
    (usage, usageTooltip, paramTooltips, returnTooltip, modifiers, typeparams, signatureTooltip, location, if staticParam.Name <> staticParam.DisplayName then Some staticParam.Name else None)

module internal Reader =
  open FSharp.Formatting.Markdown
  open FSharp.Formatting.Literate
  open ValueReader

  // ----------------------------------------------------------------------------------------------
  // Helper functions
  // ----------------------------------------------------------------------------------------------

  let removeSpaces (comment:string) =
    use reader = new StringReader(comment)
    let lines =
      [ let line = ref ""
        while (line := reader.ReadLine(); line.Value <> null) do
          yield line.Value ]
    String.removeSpaces lines

  let readMarkdownComment (doc:LiterateDocument) =
    let groups = System.Collections.Generic.Dictionary<_, _>()
    let mutable current = "<default>"
    groups.Add(current, [])
    let raw =
        match doc.Source with
        | LiterateSource.Markdown (string) -> [KeyValuePair(current, string)]
        | LiterateSource.Script _ -> []

    for par in doc.Paragraphs do
      match par with
      | Heading(2, [Literal(text, _)], _) ->
          current <- text.Trim()
          groups.Add(current, [par])
      | par ->
          groups.[current] <- par::groups.[current]
    let blurb = Literate.ToHtml(doc.With(List.rev groups.["<default>"]))
    let full = Literate.ToHtml(doc)

    let sections =
      [ for (KeyValue(k, v)) in groups ->
          let body = if k = "<default>" then List.rev v else List.tail (List.rev v)
          let html = Literate.ToHtml(doc.With(body))
          KeyValuePair(k, html) ]
    ApiDocComment(blurb, full, sections, raw)

  let findCommand = (function
    | StringPosition.StartsWithWrapped ("[", "]") (ParseCommand(k, v), rest) ->
        Some (k, v)
    | _ -> None)

  let readXmlComment (urlMap : IUrlHolder) (doc : XElement) (cmds: IDictionary<_, _>)=
   let rawData = new Dictionary<string, string>()
   let full = new StringBuilder()
   let rec readElement (e : XElement) =
     Seq.iter (fun (x : XNode) ->
      if x.NodeType = XmlNodeType.Text then
       let text = (x :?> XText).Value
       match findCommand (text, MarkdownRange.zero) with
       | Some (k,v) -> cmds.Add(k,v)
       | None -> full.Append(text) |> ignore
      elif x.NodeType = XmlNodeType.Element then
        let elem = x :?> XElement
        match elem.Name.LocalName with
        | "list" ->
          full.Append("<ul>") |> ignore
          readElement elem
          full.Append("</ul>") |> ignore
        | "item" ->
          full.Append("<li>") |> ignore
          full.Append(elem.Value) |> ignore
          full.Append("</li>") |> ignore
        | "para" ->
          full.Append("<p class='para'>") |> ignore
          full.Append(elem.Value) |> ignore
          full.Append("</p>") |> ignore
        | "see"
        | "seealso" ->
           let cref = elem.Attribute(XName.Get "cref")
           if cref <> null then
            if System.String.IsNullOrEmpty(cref.Value) || cref.Value.Length < 3 then
              failwithf "Invalid cref specified in: %A" doc
            match urlMap.ResolveCref cref.Value with
            | Some (reference) ->
              full.AppendFormat("<a href=\"{0}\">{1}</a>", reference.ReferenceLink, reference.NiceName) |> ignore
            | _ ->
              full.AppendFormat("UNRESOLVED({0})", cref.Value) |> ignore
        | "c" ->
           full.Append("<code>") |> ignore
           full.Append(elem.Value) |> ignore
           full.Append("</code>") |> ignore
        | _ ->
          ()
       ) (e.Nodes())
   readElement doc
   full.Append("</br>") |> ignore

   let summaries = doc.Descendants(XName.Get "summary")
   summaries |> Seq.iteri (fun id e ->
     let n = if id = 0 then "summary" else "summary-" + string id
     rawData.[n] <- e.Value
     full.Append("<p class='summary'>") |> ignore
     readElement e
     full.Append("</p>") |> ignore
   )

   let parameters = doc.Descendants(XName.Get "param")
   if Seq.length parameters > 0 then
     full.Append("<h4>Parameters</h4>") |> ignore
     full.Append("<dl>") |> ignore
     for e in parameters do
       let name = e.Attribute(XName.Get "name").Value
       let description = e.Value
       rawData.["param-" + name] <- description
       full.AppendFormat("<dt><span class='parameter'>{0}</span></dt><dd><p>{1}</p></dd>", name, description) |> ignore
     full.Append("</dl>") |> ignore

   let returns = doc.Descendants(XName.Get "returns")
   returns |> Seq.iteri (fun id e ->
     let n = if id = 0 then "returns" else "returns-" + string id

     full.Append("<p class='returns'>") |> ignore
     let description = e.Value
     rawData.[n] <- description
     full.AppendFormat("Returns: {0}",description) |> ignore
     full.Append("</p>") |> ignore
   )

   let exceptions = doc.Descendants(XName.Get "exception")
   if Seq.length exceptions > 0 then
     full.Append("<h4>Exceptions</h4>") |> ignore
     full.Append("<table>") |> ignore
     for e in exceptions do
       let cref = e.Attribute(XName.Get "cref")
       if cref <> null then
        if System.String.IsNullOrEmpty(cref.Value) || cref.Value.Length < 3 then
          failwithf "Invalid cref specified in: %A" doc
        match urlMap.ResolveCref cref.Value with
        | Some (reference) ->
          rawData.["exception-" + reference.NiceName] <- reference.ReferenceLink
          full.AppendFormat("<tr><td><a href=\"{0}\">{1}</a></td><td>{2}</td></tr>", reference.ReferenceLink, reference.NiceName,e.Value) |> ignore
        | _ ->
          full.AppendFormat("<tr><td>UNRESOLVED({0})</td><td></td></tr>", cref.Value) |> ignore
     full.Append("</table>") |> ignore

   let remarks = doc.Descendants(XName.Get "remarks")
   if Seq.length remarks > 0 then
     full.Append("<h4>Remarks</h4>") |> ignore
     remarks |> Seq.iteri (fun id e ->
       let n = if id = 0 then "remarks" else "remarks-" + string id
       rawData.[n] <- e.Value
       full.Append("<p class='remarks'>") |> ignore
       readElement e
       full.Append("</p>") |> ignore
     )


   doc.Descendants ()
   |> Seq.filter (fun n ->
        let ln = n.Name.LocalName
        ln <> "summary" &&
        ln <> "param" &&
        ln <> "exceptions" &&
        ln <> "returns" &&
        ln <> "remarks" )
   |> Seq.groupBy (fun n -> n.Name.LocalName)
   |> Seq.iter (fun (n, lst) ->
        let lst = Seq.toList lst
        match lst with
        | [x] -> rawData.[n] <- x.Value
        | lst ->
            lst |> Seq.iteri (fun id el ->
                rawData.[n + "-" + string id] <- el.Value)
        ()
   )

   // TODO: process param, returns tags, note that given that FSharp.Formatting infers the signature
   // via reflection this tags are not so important in F#
   let str = full.ToString()
   let raw = rawData |> Seq.toList
   ApiDocComment(str, str, [KeyValuePair("<default>", str)], raw)

  /// Returns all indirect links in a specified span node
  let rec collectSpanIndirectLinks span = seq {
    match span with
    | IndirectLink (_, _, key, _) -> yield key
    | MarkdownPatterns.SpanLeaf _ -> ()
    | MarkdownPatterns.SpanNode(_, spans) ->
      for s in spans do yield! collectSpanIndirectLinks s }

  /// Returns all indirect links in the specified paragraph node
  let rec collectParagraphIndirectLinks par = seq {
    match par with
    | MarkdownPatterns.ParagraphLeaf _ -> ()
    | MarkdownPatterns.ParagraphNested(_, pars) ->
      for ps in pars do
        for p in ps do yield! collectParagraphIndirectLinks p
    | MarkdownPatterns.ParagraphSpans(_, spans) ->
      for s in spans do yield! collectSpanIndirectLinks s }

  /// Returns whether the link is not included in the document defined links
  let linkNotDefined (doc: LiterateDocument) (link:string) =
    [ link; link.Replace("\r\n", ""); link.Replace("\r\n", " ");
      link.Replace("\n", ""); link.Replace("\n", " ") ]
    |> Seq.map (fun key -> not(doc.DefinedLinks.ContainsKey(key)) )
    |> Seq.reduce (fun a c -> a && c)

  /// Returns a tuple of the undefined link and its Cref if it exists
  let getTypeLink (ctx:ReadingContext) undefinedLink =
    // Append 'T:' to try to get the link from urlmap
    match ctx.UrlMap.ResolveCref ("T:" + undefinedLink) with
    | Some cRef -> if cRef.IsInternal then Some (undefinedLink, cRef) else None
    | None -> None

  /// Adds a cross-type link to the document defined links
  let addLinkToType (doc: LiterateDocument) link =
    match link with
    | Some(k, v) -> do doc.DefinedLinks.Add(k, (v.ReferenceLink, Some v.NiceName))
    | None -> ()

  /// Wraps the span inside an `IndirectLink` if it is an inline code that can be converted to a link
  let wrapInlineCodeLinksInSpans (ctx:ReadingContext) span =
    match span with
    | InlineCode(code, r) ->
        match getTypeLink ctx code with
        | Some _ -> IndirectLink([span], code, code, r)
        | None -> span
    | _ -> span

  /// Wraps inside an `IndirectLink` all inline code spans in the paragraph that can be converted to a link
  let rec wrapInlineCodeLinksInParagraphs (ctx:ReadingContext) (para:MarkdownParagraph) =
    match para with
    | MarkdownPatterns.ParagraphLeaf _ -> para
    | MarkdownPatterns.ParagraphNested(info, pars) ->
        MarkdownPatterns.ParagraphNested(info, pars |> List.map (fun innerPars -> List.map (wrapInlineCodeLinksInParagraphs ctx) innerPars))
    | MarkdownPatterns.ParagraphSpans(info, spans) -> MarkdownPatterns.ParagraphSpans(info, List.map (wrapInlineCodeLinksInSpans ctx) spans)

  /// Adds the missing links to types to the document defined links
  let addMissingLinkToTypes ctx (doc: LiterateDocument) =
    let replacedParagraphs = doc.Paragraphs |> List.map (wrapInlineCodeLinksInParagraphs ctx)

    do replacedParagraphs
    |> Seq.collect collectParagraphIndirectLinks
    |> Seq.filter (linkNotDefined doc)
    |> Seq.map (getTypeLink ctx)
    |> Seq.iter (addLinkToType doc)

    doc.With(paragraphs=replacedParagraphs)

  let readCommentAndCommands (ctx:ReadingContext) xmlSig =
    match ctx.XmlMemberLookup(xmlSig) with
    | None ->
        if not (System.String.IsNullOrEmpty xmlSig) then
            Log.verbf "Could not find documentation for '%s'! (You can ignore this message when you have not written documentation for this member)" xmlSig
        dict[], ApiDocComment.Empty
    | Some el ->
        let sum = el.Element(XName.Get "summary")
        match sum with
        | null when String.IsNullOrEmpty el.Value ->
          dict[], ApiDocComment.Empty
        | null ->
          dict[], (ApiDocComment("", el.Value, [], []))
        | sum ->
          let lines = removeSpaces sum.Value |> Seq.map (fun s -> (s, MarkdownRange.zero))
          let cmds = new System.Collections.Generic.Dictionary<_, _>()

          if ctx.MarkdownComments then
            let text =
              lines |> Seq.filter (findCommand >> (function
                  | Some (k, v) ->
                      cmds.Add(k, v)
                      false
                  | _ -> true)) |> Seq.map fst |> String.concat "\n"
            let doc =
                Literate.ParseMarkdownString
                  ( text, path=Path.Combine(ctx.AssemblyPath, "docs.fsx"),
                    formatAgent=ctx.FormatAgent, compilerOptions=ctx.CompilerOptions )
                  |> (addMissingLinkToTypes ctx)
            cmds :> IDictionary<_, _>, readMarkdownComment doc
          else
            lines
              |> Seq.choose findCommand
              |> Seq.iter (fun (k, v) -> cmds.Add(k,v))
            cmds :> IDictionary<_, _>, readXmlComment ctx.UrlMap el cmds

  let readComment ctx xmlSig = readCommentAndCommands ctx xmlSig |> snd


  // -----------------------------------------------------------------------
  // Hack for getting the xmldoc signature from C# assemblies
  // FSC consistently returns emtpy strings in .XmlDocSig properties
  // and as such Fsharp.formatting is unable to look up the correct
  // comment.

  let getXmlDocSigForType (typ:FSharpEntity) =
    match typ.XmlDocSig with
    | "" ->
        try
            defaultArg
                (Option.map (sprintf "T:%s") typ.TryFullName)
                ""
        with _ -> ""
    | n -> n

  let getMemberXmlDocsSigPrefix (memb:FSharpMemberOrFunctionOrValue) =
    if memb.IsEvent then "E"
    elif memb.IsProperty then "P"
    else "M"

  let getXmlDocSigForMember (memb:FSharpMemberOrFunctionOrValue) =
    match memb.XmlDocSig with
    | "" ->
        let memberName =
          try
            let name = memb.CompiledName.Replace(".ctor", "#ctor")
            let typeGenericParameters =
                memb.DeclaringEntity.Value.GenericParameters |> Seq.mapi (fun num par -> par.Name, sprintf "`%d" num)
            let methodGenericParameters =
                memb.GenericParameters |> Seq.mapi (fun num par -> par.Name, sprintf "``%d" num)
            let typeArgsMap =
                Seq.append methodGenericParameters typeGenericParameters
                |> Seq.groupBy fst
                |> Seq.map (fun (name, grp) -> grp |> Seq.head)
                |> dict
            let typeargs =
                if memb.GenericParameters.Count > 0
                then sprintf "``%d" memb.GenericParameters.Count
                else ""

            let paramList =
                if memb.CurriedParameterGroups.Count > 0 && memb.CurriedParameterGroups.[0].Count > 0
                then
                    let head = memb.CurriedParameterGroups.[0]
                    let paramTypeList =
                        head
                        |> Seq.map (fun param ->
                            if param.Type.IsGenericParameter then
                                typeArgsMap.[param.Type.GenericParameter.Name]
                            else
                                param.Type.TypeDefinition.FullName)
                    "(" + System.String.Join(", ", paramTypeList) + ")"
                else ""
            sprintf "%s%s%s" name typeargs paramList
          with exn ->
            Log.errorf "Error while building member-name for %s because: %s" memb.FullName exn.Message
            Log.verbf "Full Exception details of previous message: %O" exn
            memb.CompiledName
        match (memb.DeclaringEntity.Value.TryFullName) with
        | None    -> ""
        | Some(n)  -> sprintf "%s:%s.%s" (getMemberXmlDocsSigPrefix memb)  n memberName
    | n -> n

  //
  // ---------------------------------------------------------------------



  let getTypeProviderXmlSig (typ:FSharpEntity) =
    "T:" + typ.AccessPath + "." + typ.LogicalName

  let createUrlHolder () =
    let toReplace =
        ([("Microsoft.", ""); (".", "-"); ("`", "-"); ("<", "_"); (">", "_"); (" ", "_"); ("#", "_")] @
            (Path.GetInvalidPathChars()
            |> Seq.append (Path.GetInvalidFileNameChars())
            |> Seq.map (fun inv -> (inv.ToString(), "_")) |> Seq.toList))
        |> Seq.distinctBy fst
        |> Seq.toList
    let usedNames = Dictionary<_, _>()
    let registeredEntities = Dictionary<_, _>()
    let entityLookup = Dictionary<_, _>()
    let niceNameEntityLookup = Dictionary<_, _>()

    let nameGen (name:string) =
      let nice = (toReplace
                  |> Seq.fold (fun (s:string) (inv, repl) -> s.Replace(inv, repl)) name)
                  .ToLower()
      let found =
        seq { yield nice
              for i in Seq.initInfinite id do yield sprintf "%s-%d" nice i }
        |> Seq.find (usedNames.ContainsKey >> not)
      usedNames.Add(found, true)
      found

    let rec registerEntity (entity: FSharpEntity) =
        let newName = nameGen (sprintf "%s.%s" entity.AccessPath entity.CompiledName)
        registeredEntities.[entity] <- newName
        let xmlsig = getXmlDocSigForType entity
        if (not (System.String.IsNullOrEmpty xmlsig)) then
            assert (xmlsig.StartsWith("T:"))
            entityLookup.[xmlsig.Substring(2)] <- entity
            if (not(niceNameEntityLookup.ContainsKey(entity.LogicalName))) then
                niceNameEntityLookup.[entity.LogicalName] <- List<_>()
            niceNameEntityLookup.[entity.LogicalName].Add(entity)
        for nested in entity.NestedEntities do registerEntity nested

    let getUrl (entity:FSharpEntity) =
        match registeredEntities.TryGetValue (entity) with
        | true, v -> v
        | _ -> failwithf "The entity %s was not registered before!" (sprintf "%s.%s" entity.AccessPath entity.CompiledName)

    let removeParen (memberName:string) =
        let firstParen = memberName.IndexOf("(")
        if firstParen > 0 then memberName.Substring(0, firstParen) else memberName

    let tryGetTypeFromMemberName (memberName : string) =
        let sub = removeParen memberName
        let lastPeriod = sub.LastIndexOf(".")
        if lastPeriod > 0 then
            Some (memberName.Substring(0, lastPeriod))
        else None

    let getNoNamespaceMemberName keepParts (memberNameNoParen:string) =
        let splits = memberNameNoParen.Split([|'.'|])
        let noNamespaceParts =
            if splits.Length > keepParts then
                Array.sub splits (splits.Length - keepParts) keepParts
            else splits
        System.String.Join(".", noNamespaceParts)

    let lookupTypeCref typeName =
        match entityLookup.TryGetValue(typeName) with
        | true, entity ->
            Some { IsInternal = true; ReferenceLink = sprintf "%s.html" (getUrl entity); NiceName = entity.LogicalName }
        | _ ->
            match niceNameEntityLookup.TryGetValue(typeName) with
            | true, entityList ->
                if entityList.Count = 1 then
                    Some{ IsInternal = true; ReferenceLink = sprintf "%s.html" (getUrl entityList.[0]); NiceName = entityList.[0].LogicalName }
                else
                    if entityList.Count > 1 then
                        do Log.warnf "Duplicate types found for the simple name: %s" typeName
                    None
            | _ -> None

    let resolveCref (cref:string) =
        if (cref.Length < 2) then invalidArg "cref" (sprintf "the given cref: '%s' is invalid!" cref)
        let memberName = cref.Substring(2)
        let noParen = removeParen memberName
        match cref with
        // Type
        | _ when cref.StartsWith("T:") ->
            match lookupTypeCref (memberName) with
            | Some ref -> Some ref
            | None ->
                let simple = getNoNamespaceMemberName 1 noParen
                Some { IsInternal = false;
                       ReferenceLink = sprintf "http://msdn.microsoft.com/en-us/library/%s" noParen;
                       NiceName = simple }
        // Compiler was unable to resolve!
        | _ when cref.StartsWith("!:")  ->
            Log.warnf "Compiler was unable to resolve %s" cref
            None
        // ApiDocMember
        | _ when cref.[1] = ':' ->
            let simple = getNoNamespaceMemberName 2 noParen
            match tryGetTypeFromMemberName memberName with
            | Some typeName ->
                match lookupTypeCref typeName with
                | Some reference -> Some { reference with NiceName = simple }
                | None ->
                    Some { IsInternal = false;
                           ReferenceLink = sprintf "http://msdn.microsoft.com/en-us/library/%s" noParen;
                           NiceName = simple }
            | None ->
                Log.warnf "Assumed '%s' was a member but we cannot extract a type!" cref
                None
        // No idea
        | _ ->
            Log.warnf "Unresolved reference '%s'!" cref
            None
    { new IUrlHolder with
        member x.RegisterEntity entity =
            registerEntity entity
        member x.GetUrl entity = getUrl entity
        member x.ResolveCref cref = resolveCref cref
    }

  // ----------------------------------------------------------------------------------------------
  // Reading entities
  // ----------------------------------------------------------------------------------------------

  /// Reads XML documentation comments and calls the specified function
  /// to parse the rest of the entity, unless [omit] command is set.
  /// The function is called with category name, commands & comment.
  let readCommentsInto (sym:FSharpSymbol) ctx xmlDoc f =
    let cmds, comment = readCommentAndCommands ctx xmlDoc
    match cmds with
    | Command "omit" _ -> None
    | Command "category" cat
    | Let "" (cat, _) ->
      try
        Some(f cat cmds comment)
      with e ->
        let name =
          try sym.FullName
          with _ ->
            try sym.DisplayName
            with _ ->
              let part =
                try
                  let ass = sym.Assembly
                  match ass.FileName with
                  | Some file -> file
                  | None -> ass.QualifiedName
                with _ -> "unknown"
              sprintf "unknown, part of %s" part
        Log.errorf "Could not read comments from entity '%s': %O" name e
        None

  let checkAccess ctx (access: FSharpAccessibility) =
     not ctx.PublicOnly || access.IsPublic

  let readChildren ctx (entities:seq<FSharpEntity>) reader cond =
    entities
    |> Seq.filter (fun v -> checkAccess ctx v.Accessibility)
    |> Seq.filter cond
    |> Seq.sortBy (fun (c:FSharpEntity) -> c.DisplayName)
    |> Seq.choose (reader ctx)
    |> List.ofSeq

  let tryReadMember (ctx:ReadingContext) entityUrl kind (memb:FSharpMemberOrFunctionOrValue) =
    readCommentsInto memb ctx (getXmlDocSigForMember memb) (fun cat _ comment ->
      ApiDocMember(memb.DisplayName, readAttributes memb.Attributes, entityUrl, kind, cat, readMemberOrVal ctx memb, comment, memb))

  let readAllMembers ctx entityUrl kind (members:seq<FSharpMemberOrFunctionOrValue>) =
    members
    |> Seq.filter (fun v -> checkAccess ctx v.Accessibility)
    |> Seq.filter (fun v -> not v.IsCompilerGenerated && not v.IsPropertyGetterMethod && not v.IsPropertyGetterMethod)
    |> Seq.choose (tryReadMember ctx entityUrl kind) |> List.ofSeq

  let readMembers ctx entityUrl kind (entity:FSharpEntity) cond =
    entity.MembersFunctionsAndValues
    |> Seq.filter (fun v -> checkAccess ctx v.Accessibility)
    |> Seq.filter (fun v -> not v.IsCompilerGenerated)
    |> Seq.filter cond |> Seq.choose (tryReadMember ctx entityUrl kind) |> List.ofSeq

  let readTypeName (typ:FSharpEntity) =
    typ.GenericParameters
    |> List.ofSeq
    |> List.map (fun p -> sprintf "'%s" p.Name)
    |> function
    | [] -> typ.DisplayName
    | gnames -> sprintf "%s<%s>" typ.DisplayName (String.concat ", " gnames)

  let readUnionCases ctx entityUrl (typ:FSharpEntity) =
    typ.UnionCases
    |> List.ofSeq
    |> List.filter (fun v -> checkAccess ctx v.Accessibility)
    |> List.choose (fun case ->
      readCommentsInto case ctx case.XmlDocSig (fun cat _ comment ->
        let details = readUnionCase ctx typ case
        ApiDocMember(case.Name, readAttributes case.Attributes, entityUrl, ApiDocMemberKind.UnionCase, cat, details, comment, case)))

  let readRecordFields ctx entityUrl (typ:FSharpEntity) =
    typ.FSharpFields
    |> List.ofSeq
    |> List.filter (fun field -> not field.IsCompilerGenerated)
    |> List.choose (fun field ->
      readCommentsInto field ctx field.XmlDocSig (fun cat _ comment ->
        let details = readFSharpField ctx field
        ApiDocMember(field.Name, readAttributes (Seq.append field.FieldAttributes field.PropertyAttributes), entityUrl, ApiDocMemberKind.RecordField, cat, details, comment, field)))

  let readStaticParams ctx entityUrl (typ:FSharpEntity) =
    typ.StaticParameters
    |> List.ofSeq
    |> List.choose (fun staticParam ->
      readCommentsInto staticParam ctx (getFSharpStaticParamXmlSig typ staticParam.Name) (fun cat _ comment ->
        let details = readFSharpStaticParam ctx staticParam
        ApiDocMember(staticParam.Name, [], entityUrl, ApiDocMemberKind.StaticParameter, cat, details, comment, staticParam)))

  // ----------------------------------------------------------------------------------------------
  // Reading modules types (mutually recursive, because of nesting)
  // ----------------------------------------------------------------------------------------------


  // Create a xml documentation snippet and add it to the XmlMemberMap
  let registerXmlDoc (ctx:ReadingContext) xmlDocSig (xmlDoc:string) =
    let xmlDoc = if xmlDoc.Contains "<summary>" then xmlDoc else "<summary>" + xmlDoc + "</summary>"
    let xmlDoc = "<member name=\"" + xmlDocSig + "\">" + xmlDoc + "</member>"
    let xmlDoc = XElement.Parse xmlDoc
    ctx.XmlMemberMap.Add(xmlDocSig, xmlDoc)
    xmlDoc

  // Type providers don't have their docs dumped into the xml file,
  // so we need to add them to the XmlMemberMap separately
  let registerTypeProviderXmlDocs (ctx:ReadingContext) (typ:FSharpEntity) =
    let xmlDoc = registerXmlDoc ctx typ.XmlDocSig (String.concat "" typ.XmlDoc)
    xmlDoc.Descendants(XName.Get "param")
    |> Seq.choose (fun p -> let nameAttr = p.Attribute(XName.Get "name")
                            if nameAttr = null
                            then None
                            else Some (nameAttr.Value, p.Value))
    |> Seq.iter (fun (name, xmlDoc) ->
        let xmlDocSig = getFSharpStaticParamXmlSig typ name
        registerXmlDoc ctx xmlDocSig (Security.SecurityElement.Escape xmlDoc) |> ignore)

  let rec readModulesAndTypes ctx (entities:seq<_>) =
    let modules = readChildren ctx entities readModule (fun x -> x.IsFSharpModule)
    let types = readChildren ctx entities readType (fun x -> not x.IsFSharpModule)
    modules, types

  and readType (ctx:ReadingContext) (typ:FSharpEntity) =
    if typ.IsProvided && typ.XmlDoc.Count > 0 then
        registerTypeProviderXmlDocs ctx typ
    let xmlDocSig = getXmlDocSigForType typ
    readCommentsInto typ ctx xmlDocSig (fun cat cmds comment ->
      let entityUrl = ctx.UrlMap.GetUrl typ

      let rec getMembers (typ:FSharpEntity) = [
        yield! typ.MembersFunctionsAndValues
        match typ.BaseType with
        | Some baseType ->
            //TODO: would be better to reuse instead of reparsing the base type xml docs
            let cmds, comment = readCommentAndCommands ctx (getXmlDocSigForType baseType.TypeDefinition)
            match cmds with
            | Command "omit" _ -> yield! getMembers baseType.TypeDefinition
            | _ -> ()
        | None -> ()
      ]

      let ivals, svals =
          getMembers typ
          |> List.ofSeq
          |> List.filter (fun v -> checkAccess ctx v.Accessibility && not v.IsCompilerGenerated && not v.IsOverrideOrExplicitInterfaceImplementation)
          |> List.filter (fun v ->
            if v.DeclaringEntity.Value.IsFSharp then true else
                not v.IsEventAddMethod && not v.IsEventRemoveMethod &&
                not v.IsPropertyGetterMethod && not v.IsPropertySetterMethod)
          |> List.partition (fun v -> v.IsInstanceMember)
      let cvals, svals = svals |> List.partition (fun v -> v.CompiledName = ".ctor")

      (*
      // Base types?
      let iimpls =
        if ( not typ.IsAbbreviation && not typ.HasAssemblyCodeRepresentation &&
             typ.ReflectionType.IsInterface) then [] else typ.Implements |> List.ofSeq
      // TODO: layout base type in some way
      if not iimpls.IsEmpty then
        newTable1 hFile "Interfaces" 40 "Type"  (fun () ->
          iimpls |> List.iter (fun i ->
              newEntry1 hFile ("<pre>"+outputL widthVal (layoutType denv i)+"</pre>")))
      *)

      let name = readTypeName typ
      let cases = readUnionCases ctx entityUrl typ
      let fields = readRecordFields ctx entityUrl typ
      let statParams = readStaticParams ctx entityUrl typ

      let attrs = readAttributes typ.Attributes

      let ctors = readAllMembers ctx entityUrl ApiDocMemberKind.Constructor cvals
      let inst = readAllMembers ctx entityUrl ApiDocMemberKind.InstanceMember ivals
      let stat = readAllMembers ctx entityUrl ApiDocMemberKind.StaticMember svals

      ApiDocType (name, cat, entityUrl, comment, ctx.Assembly, attrs, cases, fields, statParams, ctors, inst, stat ))

  and readModule (ctx:ReadingContext) (modul:FSharpEntity) =
    readCommentsInto modul ctx modul.XmlDocSig (fun cat cmd comment ->

      // Properties & value bindings in the module
      let entityUrl = ctx.UrlMap.GetUrl modul
      let vals = readMembers ctx entityUrl ApiDocMemberKind.ValueOrFunction modul (fun v -> not v.IsMember && not v.IsActivePattern)
      let exts = readMembers ctx entityUrl ApiDocMemberKind.TypeExtension modul (fun v -> v.IsExtensionMember)
      let pats = readMembers ctx entityUrl ApiDocMemberKind.ActivePattern modul (fun v -> v.IsActivePattern)
      let attrs = readAttributes modul.Attributes
      // Nested modules and types
      let modules, types = readModulesAndTypes ctx modul.NestedEntities

      ApiDocModule
        ( modul.DisplayName, cat, entityUrl, comment, ctx.Assembly, attrs,
          modules, types,
          vals, exts, pats ))

  // ----------------------------------------------------------------------------------------------
  // Reading namespace and assembly details
  // ----------------------------------------------------------------------------------------------

  let readNamespace ctx (ns, entities:seq<FSharpEntity>) =
    let modules, types = readModulesAndTypes ctx entities
    ApiDocNamespace(ns, modules, types)

  let readAssembly (assembly:FSharpAssembly, publicOnly, xmlFile:string, sourceFolderRepo, urlRangeHighlight, markDownComments, urlMap, codeFormatCompilerArgs) =
    let assemblyName = AssemblyName(assembly.QualifiedName)

    // Read in the supplied XML file, map its name attributes to document text
    let doc = XDocument.Load(xmlFile)

    // don't use 'dict' to allow the dictionary to be mutated later on
    let xmlMemberMap = Dictionary()
    for key, value in
      [ for e in doc.Descendants(XName.Get "member") do
          let attr = e.Attribute(XName.Get "name")
          if attr <> null && not (String.IsNullOrEmpty(attr.Value)) then
            yield attr.Value, e ] do
        // NOTE: We completely ignore duplicate keys and I don't see
        // an easy way to detect where "value" is coming from, because the entries
        // are completely identical.
        // We just take the last here because it is the easiest to implement.
        // Additionally we log a warning just in case this is an issue in the future.
        // See https://github.com/fsprojects/FSharp.Formatting/issues/229
        // and https://github.com/fsprojects/FSharp.Formatting/issues/287
        if xmlMemberMap.ContainsKey key then
          Log.warnf "Duplicate documentation for '%s', one will be ignored!" key
        xmlMemberMap.[key] <- value

    // Code formatting agent & options used when processing inline code snippets in comments
    let asmPath = Path.GetDirectoryName(defaultArg assembly.FileName xmlFile)
    let formatAgent = CodeFormatAgent()
    let ctx =
      ReadingContext.Create
        (publicOnly, assemblyName, xmlMemberMap, sourceFolderRepo, urlRangeHighlight,
         markDownComments, urlMap, asmPath, codeFormatCompilerArgs, formatAgent)

    //
    let namespaces =
      assembly.Contents.Entities
      |> Seq.filter (fun modul -> checkAccess ctx modul.Accessibility)
      |> Seq.groupBy (fun modul -> modul.AccessPath)
      |> Seq.sortBy fst
      |> Seq.map (readNamespace ctx)
      |> List.ofSeq

    assemblyName, namespaces

/// Represents a set of assemblies integrated with their associated documentation
type ApiDocsModel =
  {
    AssemblyGroup      : ApiDocAssemblyGroup
    ModuleInfos        : ApiDocModuleInfo list
    TypesInfos         : ApiDocTypeInfo list
    Properties         : (string * IDictionary<string, string>) list
    CollectionRootUrl  : string
  }

  static member Generate(dllFiles: seq<string>, parameters, xmlFile, sourceRepo, sourceFolder, publicOnly, libDirs, otherFlags, markDownComments, urlRangeHighlight, rootUrl) =
    let (@@) a b = Path.Combine(a, b)
    let parameters = defaultArg parameters []
    let props = [ "Properties", dict parameters ]

    // Default template file names

    let otherFlags = defaultArg otherFlags []
    let publicOnly = defaultArg publicOnly true
    let libDirs = defaultArg libDirs [] |> List.map Path.GetFullPath
    let dllFiles = dllFiles |> List.ofSeq |>  List.map Path.GetFullPath
    let urlRangeHighlight =
      defaultArg urlRangeHighlight (fun url start stop -> String.Format("{0}#L{1}-{2}", url, start, stop))

    let sourceFolderRepo =
        match sourceFolder, sourceRepo with
        | Some folder, Some repo -> Some(folder, repo)
        | Some folder, _ ->
            Log.warnf "Repository url should be specified along with source folder."
            None
        | _, Some repo ->
            Log.warnf "Source folder should be specified along with repository url."
            None
        | _ -> None

    // When resolving assemblies, look in folders where all DLLs live
    AppDomain.CurrentDomain.add_AssemblyResolve(System.ResolveEventHandler(fun o e ->
      Log.verbf "Resolving assembly: %s" e.Name
      let asmName = System.Reflection.AssemblyName(e.Name)
      let asmOpt =
        dllFiles |> Seq.tryPick (fun dll ->
          let root = Path.GetDirectoryName(dll)
          let file = root @@ (asmName.Name + ".dll")
          if File.Exists(file) then
            try
                let bytes = File.ReadAllBytes(file)
                Some(System.Reflection.Assembly.Load(bytes))
            with e ->
              Log.errorf "Couldn't load Assembly\n%s\n%s" e.Message e.StackTrace
              None
          else None )
      defaultArg asmOpt null
    ))

    // Compiler arguments used when formatting code snippets inside Markdown comments
    let codeFormatCompilerArgs =
      [ for dir in libDirs do yield sprintf "-I:\"%s\"" dir
        for file in dllFiles do yield sprintf "-r:\"%s\"" file ]
      |> String.concat " "

    // Read and process assemblies and the corresponding XML files
    let assemblies =
      let resolvedList =
        //FSharpAssembly.LoadFiles(dllFiles, libDirs, otherFlags = otherFlags)
        FSharpAssembly.LoadFiles(dllFiles, libDirs, otherFlags = otherFlags, manualResolve=true)
        |> Seq.toList

      // generate the names for the html files beforehand so we can resolve <see cref=""/> links.
      let urlMap = Reader.createUrlHolder()

      resolvedList |> Seq.iter (function
        | _, Some asm ->
          asm.Contents.Entities |> Seq.iter (urlMap.RegisterEntity)
        | _ -> ())

      resolvedList |> List.choose (function
        | dllFile, None ->
          Log.warnf "**** Skipping assembly '%s' because was not found in resolved assembly list" dllFile
          None
        | dllFile, Some asm ->
          let xmlFile = defaultArg xmlFile (Path.ChangeExtension(dllFile, ".xml"))
          let xmlFileNoExt = Path.GetFileNameWithoutExtension(xmlFile)
          let xmlFileOpt =
            //Directory.EnumerateFiles(Path.GetDirectoryName(xmlFile), xmlFileNoExt + ".*")
            Directory.EnumerateFiles(Path.GetDirectoryName xmlFile)
            |> Seq.filter (fun file ->
                let fileNoExt = Path.GetFileNameWithoutExtension file
                let ext = Path.GetExtension file
                xmlFileNoExt.Equals(fileNoExt,StringComparison.OrdinalIgnoreCase)
                && ext.Equals(".xml",StringComparison.OrdinalIgnoreCase)
            )
            |> Seq.tryHead
            //|> Seq.map (fun f -> f, f.Remove(0, xmlFile.Length - 4))
            //|> Seq.tryPick (fun (f, ext) ->
            //    if ext.Equals(".xml", StringComparison.CurrentCultureIgnoreCase)
            //      then Some(f) else None)

          match xmlFileOpt with
          | None -> raise <| FileNotFoundException(sprintf "Associated XML file '%s' was not found." xmlFile)
          | Some xmlFile ->
            Reader.readAssembly
              (asm, publicOnly, xmlFile, sourceFolderRepo, urlRangeHighlight,
               defaultArg markDownComments true, urlMap, codeFormatCompilerArgs )
            |> Some)

    // Get the name - either from parameters, or name of the assembly (if there is just one)
    let name =
      let projName = parameters |> List.tryFind (fun (k, v) -> k = "project-name") |> Option.map snd
      match assemblies, projName with
      | _, Some name -> name
      | [ asm, _ ], _ -> asm.Name
      | _ -> failwith "Unknown project name. Provide 'properties' parameter with 'project-name' key."

    // Union namespaces from multiple libraries
    let namespaces = Dictionary<_, _>()
    for _, nss in assemblies do
      for ns in nss do
        match namespaces.TryGetValue(ns.Name) with
        | true, (mods, typs) -> namespaces.[ns.Name] <- (mods @ ns.Modules, typs @ ns.Types)
        | false, _ -> namespaces.Add(ns.Name, (ns.Modules, ns.Types))

    let namespaces =
      [ for (KeyValue(name, (mods, typs))) in namespaces do
          if mods.Length + typs.Length > 0 then
              ApiDocNamespace(name, mods, typs) ]

    let asm = ApiDocAssemblyGroup(name, List.map fst assemblies, namespaces |> List.sortBy (fun ns -> ns.Name))

    let rec nestedModules ns parent (modul:ApiDocModule) =
      seq {
        yield ApiDocModuleInfo(modul, asm, ns, parent)
        for n in modul.NestedModules do yield! nestedModules ns (Some modul) n
      }

    let moduleInfos =
      [ for ns in asm.Namespaces do
          for n in ns.Modules do yield! nestedModules ns None n ]

    let createType ns modul typ =
        ApiDocTypeInfo(typ, asm, ns, modul)

    let rec nestedTypes ns (modul:ApiDocModule) = seq {
      yield! (modul.NestedTypes |> List.map (createType ns (Some modul) ))
      for n in modul.NestedModules do yield! nestedTypes ns n }

    let typesInfos =
      [ for ns in asm.Namespaces do
          for n in ns.Modules do yield! nestedTypes ns n
          yield! (ns.Types |> List.map (createType ns None ))  ]

    {
      AssemblyGroup = asm
      ModuleInfos = moduleInfos
      TypesInfos = typesInfos
      Properties = props
      CollectionRootUrl = sprintf "%s/reference" rootUrl
    }

/// Represents an entry suitable for constructing a Lunr index
type ApiDocsSearchIndexEntry = {
    uri: string
    title: string
    content: string
}

[<Obsolete("Renamed to ApiDocMember", true)>]
type Member = class end
[<Obsolete("Renamed to ApiDocMemberKind", true)>]
type MemberKind = class end
[<Obsolete("Renamed to ApiDocAttribute", true)>]
type Attribute = class end
[<Obsolete("Renamed to ApiDocComment", true)>]
type DocComment = class end
[<Obsolete("Renamed to ApiDocModule", true)>]
type Module = class end
[<Obsolete("Renamed to ApiDocModuleInfo", true)>]
type ModuleInfo = class end
[<Obsolete("Renamed to ApiDocType", true)>]
type Type = class end
[<Obsolete("Renamed to ApiDocTypeInfo", true)>]
type TypeInfo = class end

