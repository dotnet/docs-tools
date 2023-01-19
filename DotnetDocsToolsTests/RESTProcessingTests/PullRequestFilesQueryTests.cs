using Xunit;
using System.Text.Json;
using DotNetDocs.Tools.RESTQueries;

namespace DotNetDocs.Tools.Tests.RESTProcessingTests;

public class PullRequestFilesQueryTests
{
    private const string successResponse =
@"[
  {
    ""sha"": ""9ec0c6207fd4116137ba48577efb86871d01aa85"",
    ""filename"": ""docs/core/testing/index.md"",
    ""status"": ""modified"",
    ""additions"": 1,
    ""deletions"": 1,
    ""changes"": 2,
    ""blob_url"": ""https://github.com/dotnet/docs/blob/2cf77bdde04a48437ba89d4bed29273032406854/docs/core/testing/index.md"",
    ""raw_url"": ""https://github.com/dotnet/docs/raw/2cf77bdde04a48437ba89d4bed29273032406854/docs/core/testing/index.md"",
    ""contents_url"": ""https://api.github.com/repos/dotnet/docs/contents/docs/core/testing/index.md?ref=2cf77bdde04a48437ba89d4bed29273032406854"",
    ""patch"": ""@@ -35,7 +35,7 @@ More information on unit testing in .NET Core projects:\n .NET Core unit test projects are supported for:\n \n - [C#](../../csharp/index.yml)\n-- [F#](../../fsharp/index.md)\n+- [F#](../../fsharp/index.yml)\n - [Visual Basic](../../visual-basic/index.md) \n \n You can also choose between:""
  },
  {
    ""sha"": ""476b8ecf706a23a1248302d61db6d9be326adf9e"",
    ""filename"": ""docs/framework/get-started/index.md"",
    ""status"": ""modified"",
    ""additions"": 1,
    ""deletions"": 1,
    ""changes"": 2,
    ""blob_url"": ""https://github.com/dotnet/docs/blob/2cf77bdde04a48437ba89d4bed29273032406854/docs/framework/get-started/index.md"",
    ""raw_url"": ""https://github.com/dotnet/docs/raw/2cf77bdde04a48437ba89d4bed29273032406854/docs/framework/get-started/index.md"",
    ""contents_url"": ""https://api.github.com/repos/dotnet/docs/contents/docs/framework/get-started/index.md?ref=2cf77bdde04a48437ba89d4bed29273032406854"",
    ""patch"": ""@@ -60,7 +60,7 @@ To develop .NET Framework apps or components, do the following:\n \n 1. If it's not preinstalled on your operating system, install the version of the .NET Framework that your app will target. The most recent production version is the .NET Framework 4.8. It is preinstalled on Windows 10 May 2019 Update, and it is available for download on earlier versions of the Windows operating system. For .NET Framework system requirements, see [System Requirements](system-requirements.md). For information on installing other versions of the .NET Framework, see [Installation Guide](../install/guide-for-developers.md). Additional .NET Framework packages are released out of band, which means that they're released on a rolling basis outside of any regular or scheduled release cycle. For information about these packages, see [The .NET Framework and Out-of-Band Releases](the-net-framework-and-out-of-band-releases.md).\n \n-2. Select the language or languages supported by the .NET Framework that you intend to use to develop your apps. A number of languages are available, including [Visual Basic](../../visual-basic/index.md), [C#](../../csharp/index.yml), [F#](../../fsharp/index.md), and [C++/CLI](/cpp/dotnet/dotnet-programming-with-cpp-cli-visual-cpp) from Microsoft. (A programming language that allows you to develop apps for the .NET Framework adheres to the [Common Language Infrastructure (CLI) specification](https://visualstudio.microsoft.com/license-terms/ecma-c-common-language-infrastructure-standards/).)\n+2. Select the language or languages supported by the .NET Framework that you intend to use to develop your apps. A number of languages are available, including [Visual Basic](../../visual-basic/index.md), [C#](../../csharp/index.yml), [F#](../../fsharp/index.yml), and [C++/CLI](/cpp/dotnet/dotnet-programming-with-cpp-cli-visual-cpp) from Microsoft. (A programming language that allows you to develop apps for the .NET Framework adheres to the [Common Language Infrastructure (CLI) specification](https://visualstudio.microsoft.com/license-terms/ecma-c-common-language-infrastructure-standards/).)\n \n 3. Select and install the development environment to use to create your apps and that supports your selected programming language or languages. The Microsoft integrated development environment (IDE) for .NET Framework apps is [Visual Studio](https://visualstudio.microsoft.com/vs/?utm_medium=microsoft&utm_source=docs.microsoft.com&utm_campaign=inline+link). It's available in a number of editions.\n ""
  },
  {
    ""sha"": ""508eb675ba9026b7739e78a3391998d11ad65724"",
    ""filename"": ""docs/fsharp/get-started/get-started-visual-studio.md"",
    ""status"": ""modified"",
    ""additions"": 1,
    ""deletions"": 1,
    ""changes"": 2,
    ""blob_url"": ""https://github.com/dotnet/docs/blob/2cf77bdde04a48437ba89d4bed29273032406854/docs/fsharp/get-started/get-started-visual-studio.md"",
    ""raw_url"": ""https://github.com/dotnet/docs/raw/2cf77bdde04a48437ba89d4bed29273032406854/docs/fsharp/get-started/get-started-visual-studio.md"",
    ""contents_url"": ""https://api.github.com/repos/dotnet/docs/contents/docs/fsharp/get-started/get-started-visual-studio.md?ref=2cf77bdde04a48437ba89d4bed29273032406854"",
    ""patch"": ""@@ -53,7 +53,7 @@ Congratulations!  You've created your first F# project in Visual Studio, written\n \n ## Next steps\n \n-If you haven't already, check out the [Tour of F#](../tour.md), which covers some of the core features of the F# language.  It will give you an overview of some of the capabilities of F#, and provide ample code samples that you can copy into Visual Studio and run.  There are also some great external resources you can use, showcased in the [F# Guide](../index.md).\n+If you haven't already, check out the [Tour of F#](../tour.md), which covers some of the core features of the F# language.  It will give you an overview of some of the capabilities of F#, and provide ample code samples that you can copy into Visual Studio and run.  You can also learn more about the F# documentation in the [F# docs homepage](../index.yml).\n \n ## See also\n ""
  },
  {
    ""sha"": ""e753a8425df09a5dae1b6a1d859bf1ea612b395e"",
    ""filename"": ""docs/fsharp/get-started/get-started-with-visual-studio-for-mac.md"",
    ""status"": ""modified"",
    ""additions"": 2,
    ""deletions"": 2,
    ""changes"": 4,
    ""blob_url"": ""https://github.com/dotnet/docs/blob/2cf77bdde04a48437ba89d4bed29273032406854/docs/fsharp/get-started/get-started-with-visual-studio-for-mac.md"",
    ""raw_url"": ""https://github.com/dotnet/docs/raw/2cf77bdde04a48437ba89d4bed29273032406854/docs/fsharp/get-started/get-started-with-visual-studio-for-mac.md"",
    ""contents_url"": ""https://api.github.com/repos/dotnet/docs/contents/docs/fsharp/get-started/get-started-with-visual-studio-for-mac.md?ref=2cf77bdde04a48437ba89d4bed29273032406854"",
    ""patch"": ""@@ -105,11 +105,11 @@ This is only a glimpse into what you can do with F# Interactive.  To learn more,\n \n ## Next steps\n \n-If you haven't already, check out the [Tour of F#](../tour.md), which covers some of the core features of the F# language.  It will give you an overview of some of the capabilities of F#, and provide ample code samples that you can copy into Visual Studio for Mac and run.  There are also some great external resources you can use, showcased in the [F# Guide](../index.md).\n+If you haven't already, check out the [Tour of F#](../tour.md), which covers some of the core features of the F# language.  It will give you an overview of some of the capabilities of F#, and provide ample code samples that you can copy into Visual Studio for Mac and run.  There are also some great external resources you can use, showcased in the [F# Guide](../index.yml).\n \n ## See also\n \n-- [Visual F#](../index.md)\n+- [F# guide](../index.yml)\n - [Tour of F#](../tour.md)\n - [F# language reference](../language-reference/index.md)\n - [Type inference](../language-reference/type-inference.md)""
  },
  {
    ""sha"": ""9864be649578cef449c35e97208476f8bd46a9e3"",
    ""filename"": ""docs/fsharp/index.md"",
    ""status"": ""removed"",
    ""additions"": 0,
    ""deletions"": 61,
    ""changes"": 61,
    ""blob_url"": ""https://github.com/dotnet/docs/blob/dc552715d4717a9656e2e740157d7797d41b43d9/docs/fsharp/index.md"",
    ""raw_url"": ""https://github.com/dotnet/docs/raw/dc552715d4717a9656e2e740157d7797d41b43d9/docs/fsharp/index.md"",
    ""contents_url"": ""https://api.github.com/repos/dotnet/docs/contents/docs/fsharp/index.md?ref=dc552715d4717a9656e2e740157d7797d41b43d9"",
    ""patch"": ""@@ -1,61 +0,0 @@\n----\n-title: F# Guide\n-description: This guide provides an overview of various learning materials for F#, a functional programming language that runs on .NET.\n-author: cartermp\n-ms.date: 08/03/2018\n----\n-# F# Guide\n-\n-The F# guide provides many resources to learn the F# language.\n-\n-## Learning F\\#\n-\n-[What is F#](what-is-fsharp.md) describes what the F# language is and what programming in it is like, with short code samples. This is recommended if you are new to F#.\n-\n-[Tour of F#](tour.md) gives an overview of major language features with lots of code samples. This is recommended if you are interested in seeing core F# features in action.\n-\n-[Get started with F# in Visual Studio](./get-started/get-started-visual-studio.md) if you're on Windows and want the full Visual Studio IDE (Integrated Development Environment) experience.\n-\n-[Get started with F# in Visual Studio for Mac](./get-started/get-started-with-visual-studio-for-mac.md) if you're on macOS and want to use a Visual Studio IDE.\n-\n-[Get Started with F# in Visual Studio Code](./get-started/get-started-vscode.md) if you want a lightweight, cross-platform, and feature-packed IDE experience.\n-\n-[Get started with F# with the .NET Core CLI](./get-started/get-started-command-line.md) if you want to use command-line tools.\n-\n-[Get started with F# and Xamarin](https://docs.microsoft.com/xamarin/cross-platform/platform/fsharp/) for mobile programming with F#.\n-\n-[F# for Azure Notebooks](https://notebooks.azure.com/Microsoft/libraries/samples/html/FSharp%20for%20Azure%20Notebooks.ipynb) is a tutorial for learning F# in a free, hosted Jupyter Notebook.\n-\n-## References\n-\n-[F# Language Reference](./language-reference/index.md) is the official, comprehensive reference for all F# language features. Each article explains the syntax and shows code samples. You can use the filter bar in the table of contents to find specific articles.\n-\n-[F# Core Library Reference](https://msdn.microsoft.com/visualfsharpdocs/conceptual/fsharp-core-library-reference) is the API reference for the F# Core Library.\n-\n-## Additional guides\n-\n-[F# for Fun and Profit](https://swlaschin.gitbooks.io/fsharpforfunandprofit/content/) is a comprehensive and very detailed book on learning F#. Its contents and author are beloved by the F# community. The target audience is primarily developers with an object oriented programming background.\n-\n-[F# Programming Wikibook](https://en.wikibooks.org/wiki/F_Sharp_Programming) is a wikibook about learning F#. It is also a product of the F# community. The target audience is people who are new to F#, with a little bit of object oriented programming background.\n-\n-## Learn F# through videos\n-\n-[F# tutorial on YouTube](https://www.youtube.com/watch?v=c7eNDJN758U) is a great introduction to F# using Visual Studio, showing lots of great examples over the course of 1.5 hours. The target audience is Visual Studio developers who are new to F#.\n-\n-[Introduction to Programming with F#](https://www.youtube.com/watch?v=Teak30_pXHk&list=PLEoMzSkcN8oNiJ67Hd7oRGgD1d4YBxYGC) is a great video series that uses Visual Studio Code as the main editor. The video series starts from nothing and ends with building a text-based RPG video game. The target audience is developers who prefer Visual Studio Code (or a lightweight IDE) and are new to F#.\n-\n-[What's New in Visual Studio 2017 for F# For Developers](https://www.linkedin.com/learning/what-s-new-in-visual-studio-2017-for-f-sharp-for-developers) is a video course that shows some of the newer features for F# in Visual Studio 2017. The target audience is Visual Studio developers who are new to F#.\n-\n-## Other useful resources\n-\n-The [F# Snippets Website](http://www.fssnip.net) contains a massive set of code snippets showing how to do just about anything in F#, ranging from absolute beginner to highly advanced snippets.\n-\n-The [F# Software Foundation Slack](https://fsharp.org/guides/slack/) is a great place for beginners and experts alike, is highly active, and has some of world's best F# programmers available for a chat. We highly recommend joining.\n-\n-## The F# Software Foundation\n-\n-Although Microsoft is the primary developer of the F# language and its tools in Visual Studio, F# is also backed by an independent foundation, the F# Software Foundation (FSSF).\n-\n-The mission of the F# Software Foundation is to promote, protect, and advance the F# programming language, and to support and facilitate the growth of a diverse and international community of F# programmers.\n-\n-To learn more and get involved, check out [fsharp.org](https://fsharp.org). It's free to join, and the network of F# developers in the foundation is something you don't want to miss out on!""
  },
  {
    ""sha"": ""fcd3e7df71fb6c991fe183ce656c0834429c46b7"",
    ""filename"": ""docs/fsharp/index.yml"",
    ""status"": ""added"",
    ""additions"": 68,
    ""deletions"": 0,
    ""changes"": 68,
    ""blob_url"": ""https://github.com/dotnet/docs/blob/2cf77bdde04a48437ba89d4bed29273032406854/docs/fsharp/index.yml"",
    ""raw_url"": ""https://github.com/dotnet/docs/raw/2cf77bdde04a48437ba89d4bed29273032406854/docs/fsharp/index.yml"",
    ""contents_url"": ""https://api.github.com/repos/dotnet/docs/contents/docs/fsharp/index.yml?ref=2cf77bdde04a48437ba89d4bed29273032406854"",
    ""patch"": ""@@ -0,0 +1,68 @@\n+### YamlMime:Landing\n+\n+title: \""F# documentation\""\n+summary: \""Learn how to write any application using the F# programming language on .NET.\""\n+\n+metadata:\n+  title: \""F# docs - get started, tutorials, reference.\""\n+  description: \""Learn F# programming - for developers new to F#, and experienced F# / .NET developers\""\n+  ms.topic: landing-page # Required\n+  author: cartermp\n+  ms.author: phcart\n+  ms.date: 11/25/2019\n+\n+landingContent:\n+  - title: \""Learn to program in F#\""\n+    linkLists:\n+      - linkListType: get-started\n+        links:\n+          - text: \""What is F#?\""\n+            url: what-is-fsharp.md\n+          - text: \""Install F#\""\n+            url: get-started/install-fsharp.md\n+          - text: \""Get started with F# in Visual Studio\""\n+            url: get-started/get-started-visual-studio.md\n+          - text: \""Get started with F# in Visual Studio Code\""\n+            url: get-started/get-started-vscode.md\n+      - linkListType: download\n+        links:\n+          - text: Download the .NET Core SDK\n+            url: https://dotnet.microsoft.com/download\n+\n+  - title: \""F# fundamentals\""\n+    linkLists:\n+      - linkListType: overview\n+        links:\n+          - text: \""Tour of F#\""\n+            url: tour.md\n+          - text: \""Introduction to Functional Programming in F#\""\n+            url: introduction-to-functional-programming/index.md\n+          - text: \""First-class functions\""\n+            url: introduction-to-functional-programming/first-class-functions.md\n+      - linkListType: concept\n+        links:\n+          - text: \""F# style guide\""\n+            url: style-guide/index.md\n+          - text: \""F# code formatting guidelines\""\n+            url: style-guide/formatting.md\n+          - text: \""F# coding conventions\""\n+            url: style-guide/conventions.md\n+      - linkListType: tutorial\n+        links:\n+          - text: \""Async programming in F#\""\n+            url: tutorials/asynchronous-and-concurrent-programming/async.md\n+\n+  - title: \""F# language reference\""\n+    linkLists:\n+      - linkListType: reference\n+        links:\n+          - text: \""Language reference\""\n+            url: language-reference/index.md\n+          - text: \""Keyword reference\""\n+            url: language-reference/keyword-reference.md\n+          - text: \""Symbol and operator reference\""\n+            url: language-reference/symbol-and-operator-reference/index.md\n+          - text: \""Functions\""\n+            url: language-reference/functions/index.md\n+          - text: \""F# types\""\n+            url: language-reference/fsharp-types.md""
  },
  {
    ""sha"": ""58e3474e551d7cbd9b6cf65268f915e33bb9d957"",
    ""filename"": ""docs/fsharp/language-reference/index.md"",
    ""status"": ""modified"",
    ""additions"": 0,
    ""deletions"": 4,
    ""changes"": 4,
    ""blob_url"": ""https://github.com/dotnet/docs/blob/2cf77bdde04a48437ba89d4bed29273032406854/docs/fsharp/language-reference/index.md"",
    ""raw_url"": ""https://github.com/dotnet/docs/raw/2cf77bdde04a48437ba89d4bed29273032406854/docs/fsharp/language-reference/index.md"",
    ""contents_url"": ""https://api.github.com/repos/dotnet/docs/contents/docs/fsharp/language-reference/index.md?ref=2cf77bdde04a48437ba89d4bed29273032406854"",
    ""patch"": ""@@ -104,7 +104,3 @@ The following table lists topics that describe special compiler-supported constr\n |[Compiler Options](compiler-options.md)|Describes the command-line options for the F# compiler.|\n |[Compiler Directives](compiler-directives.md)|Describes processor directives and compiler directives.|\n |[Source Line, File, and Path Identifiers](source-line-file-path-identifiers.md)|Describes the identifiers `__LINE__`, `__SOURCE_DIRECTORY__` and `__SOURCE_FILE__`, which are built-in values that enable you to access the source line number, directory and file name in your code.|\n-\n-## See also\n-\n-- [Visual F#](../index.md)""
  },
  {
    ""sha"": ""e71495842075158dd0258da045aad3a5d1f7974b"",
    ""filename"": ""docs/fsharp/toc.yml"",
    ""status"": ""modified"",
    ""additions"": 2,
    ""deletions"": 2,
    ""changes"": 4,
    ""blob_url"": ""https://github.com/dotnet/docs/blob/2cf77bdde04a48437ba89d4bed29273032406854/docs/fsharp/toc.yml"",
    ""raw_url"": ""https://github.com/dotnet/docs/raw/2cf77bdde04a48437ba89d4bed29273032406854/docs/fsharp/toc.yml"",
    ""contents_url"": ""https://api.github.com/repos/dotnet/docs/contents/docs/fsharp/toc.yml?ref=2cf77bdde04a48437ba89d4bed29273032406854"",
    ""patch"": ""@@ -1,5 +1,5 @@\n-- name: F# guide\n-  href: index.md\n+- name: F# documentation\n+  href: index.yml\n - name: Get Started\n   href: get-started/index.md\n   items:""
  },
  {
    ""sha"": ""787e2201197c67a78ba1362bf76724bb811bfcff"",
    ""filename"": ""docs/fsharp/tutorials/type-providers/index.md"",
    ""status"": ""modified"",
    ""additions"": 0,
    ""deletions"": 1,
    ""changes"": 1,
    ""blob_url"": ""https://github.com/dotnet/docs/blob/2cf77bdde04a48437ba89d4bed29273032406854/docs/fsharp/tutorials/type-providers/index.md"",
    ""raw_url"": ""https://github.com/dotnet/docs/raw/2cf77bdde04a48437ba89d4bed29273032406854/docs/fsharp/tutorials/type-providers/index.md"",
    ""contents_url"": ""https://api.github.com/repos/dotnet/docs/contents/docs/fsharp/tutorials/type-providers/index.md?ref=2cf77bdde04a48437ba89d4bed29273032406854"",
    ""patch"": ""@@ -35,4 +35,3 @@ Where necessary, you can [create your own custom type providers](creating-a-type\n \n - [Tutorial: Create a Type Provider](creating-a-type-provider.md)\n - [F# Language Reference](../../language-reference/index.md)\n-- [Visual F#](../../index.md)""
  },
  {
    ""sha"": ""a921c2fc292ddd0329d1d13dab041c28f8dcd0b9"",
    ""filename"": ""docs/standard/components.md"",
    ""status"": ""modified"",
    ""additions"": 1,
    ""deletions"": 1,
    ""changes"": 2,
    ""blob_url"": ""https://github.com/dotnet/docs/blob/2cf77bdde04a48437ba89d4bed29273032406854/docs/standard/components.md"",
    ""raw_url"": ""https://github.com/dotnet/docs/raw/2cf77bdde04a48437ba89d4bed29273032406854/docs/standard/components.md"",
    ""contents_url"": ""https://api.github.com/repos/dotnet/docs/contents/docs/standard/components.md?ref=2cf77bdde04a48437ba89d4bed29273032406854"",
    ""patch"": ""@@ -105,5 +105,5 @@ The official ISO/IEC documents are available from the ISO/IEC [Publicly Availabl\n - [.NET Core Guide](../core/index.md)\n - [.NET Framework Guide](../framework/index.md)\n - [C# Guide](../csharp/index.yml)\n-- [F# Guide](../fsharp/index.md)\n+- [F# Guide](../fsharp/index.yml)\n - [VB.NET Guide](../visual-basic/index.md)""
  },
  {
    ""sha"": ""1222649dc8897f6dd9c0d8136cabdd059b310268"",
    ""filename"": ""docs/standard/index.md"",
    ""status"": ""modified"",
    ""additions"": 1,
    ""deletions"": 1,
    ""changes"": 2,
    ""blob_url"": ""https://github.com/dotnet/docs/blob/2cf77bdde04a48437ba89d4bed29273032406854/docs/standard/index.md"",
    ""raw_url"": ""https://github.com/dotnet/docs/raw/2cf77bdde04a48437ba89d4bed29273032406854/docs/standard/index.md"",
    ""contents_url"": ""https://api.github.com/repos/dotnet/docs/contents/docs/standard/index.md?ref=2cf77bdde04a48437ba89d4bed29273032406854"",
    ""patch"": ""@@ -52,7 +52,7 @@ If you're interested in some of the major concepts of .NET, check out:\n Additionally, check out each language guide to learn about the three major .NET languages:\n \n * [C# Guide](../csharp/index.yml)\n-* [F# Guide](../fsharp/index.md)\n+* [F# Guide](../fsharp/index.yml)\n * [Visual Basic Guide](../visual-basic/index.md)\n \n ## API Reference""
  },
  {
    ""sha"": ""f479a082d5b25ca5db318a2dfd893c4968ffdb43"",
    ""filename"": ""docs/standard/tour.md"",
    ""status"": ""modified"",
    ""additions"": 1,
    ""deletions"": 1,
    ""changes"": 2,
    ""blob_url"": ""https://github.com/dotnet/docs/blob/2cf77bdde04a48437ba89d4bed29273032406854/docs/standard/tour.md"",
    ""raw_url"": ""https://github.com/dotnet/docs/raw/2cf77bdde04a48437ba89d4bed29273032406854/docs/standard/tour.md"",
    ""contents_url"": ""https://api.github.com/repos/dotnet/docs/contents/docs/standard/tour.md?ref=2cf77bdde04a48437ba89d4bed29273032406854"",
    ""patch"": ""@@ -25,7 +25,7 @@ Microsoft actively develops and supports three .NET languages: C#, F#, and Visua\n \n * C# is simple, powerful, type-safe, and object-oriented, while retaining the expressiveness and elegance of C-style languages. Anyone familiar with C and similar languages finds few problems in adapting to C#. Check out the [C# Guide](../csharp/index.yml) to learn more about C#.\n \n-* F# is a cross-platform, functional-first programming language that also supports traditional object-oriented and imperative programming. Check out the [F# Guide](../fsharp/index.md) to learn more about F#.\n+* F# is a cross-platform, functional-first programming language that also supports traditional object-oriented and imperative programming. Check out the [F# Guide](../fsharp/index.yml) to learn more about F#.\n \n * Visual Basic is an easy language to learn that you use to build a variety of apps that run on .NET. Among the .NET languages, the syntax of VB is the closest to ordinary human language, often making it easier for people new to software development.\n ""
  },
  {
    ""sha"": ""5502e5e61c1aaa782d77a2f811eeafaeaadbc336"",
    ""filename"": ""docs/welcome.md"",
    ""status"": ""modified"",
    ""additions"": 1,
    ""deletions"": 1,
    ""changes"": 2,
    ""blob_url"": ""https://github.com/dotnet/docs/blob/2cf77bdde04a48437ba89d4bed29273032406854/docs/welcome.md"",
    ""raw_url"": ""https://github.com/dotnet/docs/raw/2cf77bdde04a48437ba89d4bed29273032406854/docs/welcome.md"",
    ""contents_url"": ""https://api.github.com/repos/dotnet/docs/contents/docs/welcome.md?ref=2cf77bdde04a48437ba89d4bed29273032406854"",
    ""patch"": ""@@ -42,7 +42,7 @@ This documentation covers the breadth of .NET across platforms and languages. Yo\n - [.NET Core Guide](core/index.md)\n - [.NET Framework Guide](framework/index.md)\n - [C# Guide](csharp/index.yml)\n-- [F# Guide](fsharp/index.md)\n+- [F# Guide](fsharp/index.yml)\n - [Visual Basic Guide](visual-basic/index.md)\n - [ML.NET Guide](machine-learning/index.yml)\n - [.NET for Apache Spark](spark/index.yml)""
  }
]";

    private const string failedResponse =
@"{
  ""message"": ""Not Found"",
  ""documentation_url"": ""https://developer.github.com/v3/pulls/#list-pull-requests-files""
}";
    [Fact]
    public async Task CanIterateResultsOnSuccessfulQuery()
    {
        var responseDoc = JsonDocument.Parse(successResponse);
        var client = new FakeGitHubClient(responseDoc);

        var prFiles = new PullRequestFilesRequest(client, "dotnet", "docs", 15979);

        var result = await prFiles.PerformQueryAsync();
        Assert.True(result);

        // iterate:
        foreach(var node in prFiles.Files)
        {
            Assert.NotEmpty(node.Sha);
            Assert.NotEmpty(node.Filename);
            Assert.NotEqual(-1, (int)node.Status);
            var changes = node.Additions + node.Deletions;
            Assert.Equal(changes, node.Changes);
            Assert.NotNull(node.BlobUrl);
            Assert.NotNull(node.RawUrl);
            Assert.NotNull(node.ContentsUrl);
            Assert.NotNull(node.Patch);
        }
    }

    [Fact]
    public async Task CanAccessMessageOnFailedQuery()
    {
        var responseDoc = JsonDocument.Parse(failedResponse);
        var client = new FakeGitHubClient(responseDoc);

        var prFiles = new PullRequestFilesRequest(client, "dotnet", "docs", 25000);

        var result = await prFiles.PerformQueryAsync();
        Assert.False(result);
        Assert.Equal("Not Found", prFiles.ErrorMessage);
    }

    // 3. Query more than once
    [Fact]
    public async Task RepeatedQueryRequestsExecuteAgain()
    {
        var responseDoc = JsonDocument.Parse(failedResponse);
        var client = new FakeGitHubClient(responseDoc);

        var prFiles = new PullRequestFilesRequest(client, "dotnet", "docs", 25000);

        var result = await prFiles.PerformQueryAsync();
        Assert.False(result);
        Assert.Equal("Not Found", prFiles.ErrorMessage);

        result = await prFiles.PerformQueryAsync();
        Assert.False(result);
        Assert.Equal("Not Found", prFiles.ErrorMessage);
    }

    [Fact]
    public async Task ErrorMessageThrowsOnSuccessfulQuery()
    {
        var responseDoc = JsonDocument.Parse(successResponse);
        var client = new FakeGitHubClient(responseDoc);

        var prFiles = new PullRequestFilesRequest(client, "dotnet", "docs", 25000);

        var result = await prFiles.PerformQueryAsync();
        Assert.True(result);
        Assert.Throws<InvalidOperationException>(() => prFiles.ErrorMessage);
    }
    
    [Fact]
    public async Task IterationThrowsOnFailedQuery()
    {
        var responseDoc = JsonDocument.Parse(failedResponse);
        var client = new FakeGitHubClient(responseDoc);

        var prFiles = new PullRequestFilesRequest(client, "dotnet", "docs", 25000);

        var result = await prFiles.PerformQueryAsync();
        Assert.False(result);

        Assert.Throws<InvalidOperationException>(() => prFiles.Files.GetEnumerator());
    }

    [Fact]
    public void AccessingResultsBeforeQueryThrows()
    {
        var client = new FakeGitHubClient();
        var prFiles = new PullRequestFilesRequest(client, "dotnet", "docs", 25000);

        Assert.Throws<InvalidOperationException>(() => prFiles.ErrorMessage);
        Assert.Throws<InvalidOperationException>(() => prFiles.Files);
    }

    [Fact]
    public void GitHubClientMustNotBeNull()
    {
        Assert.Throws<ArgumentNullException>(() => new PullRequestFilesRequest(default!, "dotnet", "docs", 15));
    }

    [Fact]
    public void OwnerComponentMustBeValidURLComponent()
    {
        var client = new FakeGitHubClient();
        Assert.Throws<ArgumentException>(() => new PullRequestFilesRequest(client, null!, "docs", 15));
        Assert.Throws<ArgumentException>(() => new PullRequestFilesRequest(client, "", "docs", 15));
        Assert.Throws<ArgumentException>(() => new PullRequestFilesRequest(client, "    ", "docs", 15));
    }

    [Fact]
    public void RepositoryComponentMustBeValidURLComponent()
    {
        var client = new FakeGitHubClient();
        Assert.Throws<ArgumentException>(() => new PullRequestFilesRequest(client, "dotnet", null!, 15));
        Assert.Throws<ArgumentException>(() => new PullRequestFilesRequest(client, "dotnet", "", 15));
        Assert.Throws<ArgumentException>(() => new PullRequestFilesRequest(client, "dotnet", "    ", 15));
    }

    [Fact]
    public void PRNumberComponentMustBePositive()
    {
        var client = new FakeGitHubClient();
        Assert.Throws<ArgumentOutOfRangeException>(() => new PullRequestFilesRequest(client, "dotnet", "docs", -25));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PullRequestFilesRequest(client, "dotnet", "docs", 0));
    }
}
