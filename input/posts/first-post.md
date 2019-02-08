Title: How to do cross platform compilation with MsBuild.Sdk.Extras and Xamarin.
Published: 29/1/2019
Tags:
- Xamarin
- Azure
- DevOps
- MsBuild.Sdk.Extras
---

Back in August, 2018 I took over the day to day management of the ReactiveUI project. ReactiveUI is a cross platform functional style application with support for the Reactive Extensions and DotNet. We support a lot of different platforms which adds complexity when it comes to building and testing our product.

I have learned a lot over that time. In particular we have made a lot of focus on making our CI and DevOps experience much easier for the users to contribute to our project.

I want to share with you our approach on how we handled supporting six very active repositories that all have to support .Net Standard, .Net Core and Xamarin as targets.

## Handling the TargetFramework's inside your csproj files

Overall, we support numerous Xamarin and .Net targets including .Net Core, iOS, Android, Tizen, TVOS, WPF and Winforms. We wanted to keep the new .csproj format due to it's numerous advantages but a lot of these `TargetFramework` aren't supported out of the box by the standard MSBuild SDK.

To assist with this Oren Novotny [MSBuild.Sdk.Extras](https://github.com/onovotny/MSBuildSdkExtras) comes to the rescue. It is a plugin based on the standard MSBuild SDK and expands the support with additional `TargetFramework`.

To get started at the location of your .sln file add a file called `global.json`. This JSON file is used to determine the version of MSBuild.Sdk.Extras NuGet to use throughout your projects in the same sub-folders as the file. Place the following contents inside the file:

```json
{
    "msbuild-sdks": {
        "MSBuild.Sdk.Extras": "1.6.65"
    }
}
```

Next on top of each of your .csproj replace (making sure they are the new Visual Studio 2017 csproj format):

```xml
<Project Sdk="Microsoft.NET.Sdk">
```

with

```xml
<Project Sdk="MSBuild.Sdk.Extras">
```

If you aren't targetting .NET Core 3.0 preview, For WPF and Winforms you need to include either `ExtrasEnableWpfProjectSetup` or `ExtrasEnableWinFormsProjectSetup` as a entry with a value of `true` in your csproj's main PropertyGroup. These flags will also make MSBuild.Sdk.Extras include many common references so you don't need to include references to `PresentationFramework` for example for WPF.

```xml
  <PropertyGroup>  
    <!-- Other entries -->
    <TargetFrameworks>net461</TargetFrameworks>
    <!-- Needed for WPF -->
    <ExtrasEnableWpfProjectSetup>true</ExtrasEnableWpfProjectSetup>
    <!-- Needed for Winforms -->
    <ExtrasEnableWinFormsProjectSetup>true</ExtrasEnableWinFormsProjectSetup>
  </PropertyGroup>  
```

Most of the Xamarin projects will require MsBuild to compile rather than using `dotnet build` commands.

## Handling compiling on non-windows platforms with .NET framework TargetFramework

In our projects we target of the .NET Framework and UWP `TargetFramework`. We want to do CI testing against Widows and Mac (the two platforms that support Xamarin targets). We had to find a way to not include those when not compiling on Windows.

The best way we found for allow building on Mac is to use a condition inside your `TargetFrameworks` entry that indicates to only include these items if the current build OS is Windows.

```xml
<Project Sdk="MSBuild.Sdk.Extras">
  <PropertyGroup>
    <TargetFrameworks>MonoAndroid81;Xamarin.iOS10;Xamarin.Mac20;Xamarin.TVOS10;Xamarin.WatchOS10;tizen40;netstandard2.0</TargetFrameworks>
    <TargetFrameworks Condition=" '$(OS)' == 'Windows_NT' ">$(TargetFrameworks);net461;uap10.0.16299</TargetFrameworks>
  </PropertyGroup>
</Project>
```

We found with this approach on Visual Studio for Mac only uses the first TargetFramework found in the csproj and ignores the other. We found Rider or using MsBuild worked better.

Also if you need to know if you're compiling on Mac/Linux then this [article](https://github.com/Microsoft/msbuild/issues/539) will include information.

## Handling multiple platform code without needing macro trickery

In the old days of .NET development if you needed code to compile on multiple platforms you would often use `#if` statements through your code. This isn't very sustainable.

The way we found around this is to use a platform folder in your project.

For example a common structure folder might be:

```
|
| - src
|    - Platforms
|       - ios
|       - net4
|       - wpf
|       - winforms
|       - uwp
|    - cross-platform-code
|    - more-platform-common-code
```

We take advantage of the fact that .csproj files are parsed in order. Firstly we add a ItemGroup which will exclude all files in the platform folder from being compiled (we will add our platforms back later):

```xml
  <ItemGroup>
    <Compile Remove="Platforms\**\*.*" />
    <EmbeddedResource Remove="Platforms\**\*.*" />

    <!-- Workaround so the files appear in VS -->
    <None Include="Platforms\**\*.*" />
    <None Include="Colors\**\*.*" />
  </ItemGroup>
```

Now since MsBuild sets the `TargetFramework` property for each platform it compiles, we can take advantage of this and create a item group with a condition that the `TargetFramework` starts with the prefix. We use `StartsWith` to allow it easier to migrate to new versions of the platforms in the future.

```xml
<ItemGroup Condition=" $(TargetFramework.StartsWith('netstandard')) ">
   <Compile Include="Platforms\netstandard\**\*.cs" />
<ItemGroup>

<ItemGroup Condition=" $(TargetFramework.StartsWith('net4')) ">
   <Compile Include="Platforms\net4\**\*.cs" />
<ItemGroup>
```

Some platforms are a little bit trickier like UWP where we want to also include XAML based pages. We have to tell the compiler to also include those resources as well.

```xml
 <ItemGroup Condition=" $(TargetFramework.StartsWith('uap')) ">
    <Compile Include="Platforms\uwp\**\*.cs" />

    <Page Include="Platforms\uwp\**\*.xaml" SubType="Designer" Generator="MSBuild:Compile" />
    <None Update="**\*.xaml.cs" DependentUpon="%(Filename)" />
    <Compile Update="**\*.xaml.cs" DependentUpon="%(Filename)" />
  </ItemGroup>
```

The `DependentUpon` flag uses the `%(FileName)` item which extracts the file name without the extension. So it indicates those files are dependent on their .xaml counterpart.

## Overview

I hope this article gets you started on how you can approach libraries that target multiple platforms. We found by using the above libraries and approaches it dramatically decreased our efforts in maintaining our platform specific code.