# ResXFileExtenderSourceGenerator

A very simple SourceGenerator which is extending existing ResXGenerator by adding a *.Resource.Extensions namespace containig ResourceClass which is using Methods which take culture parameter indead of Properties that use a single defined "Culture" property.

Usage:

<ItemGroup>
  <PackageReference Include="En3Tho.ResXFileExtenderSourceGenerator" Version="1.0.1" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
