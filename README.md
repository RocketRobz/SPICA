# SPICA [![Build status](https://ci.appveyor.com/api/projects/status/ar1fyeo109v587xf/branch/master?svg=true)](https://ci.appveyor.com/project/gdkchan/spica/branch/master)
Experimental H3D tool for serializing/deserializing BCH.

Dependencies:
- OpenTK
- OpenTK.GLControl

Both can be found on NuGet.

Note: The version of OpenTK.GLControl on NuGet is broken, so it's recommended to build it yourself from source and manually add a reference to the compiled library.
OpenTK git can be found [here](https://github.com/opentk/opentk).

You will need .NET Framework 4.6 and a GPU capable of OpenGL 3.3 at least.

SPICA can be built on Linux/Mac using [Mono](https://www.mono-project.com/).
See `README.mono.md` for details.

**Windows build:**

To download the lastest automatic build for Windows, [Click Here](https://ci.appveyor.com/api/projects/gdkchan/spica/artifacts/spica_lastest.zip).

** Read me if you are here for Pokemon models **

This tool allows for converting OBJ models and nothing else into BCH binaries that somewhat work with Pokemon OR/AS (read: NOT XY or SM). Requirements are:
-One material = one mesh
-Materials must be in the .mtl file and the textures must exist. Otherwise, crashes will happen.

Usage:
-Grab a working game model (some don't work but f.e. file_1.gr from a/0/3/9 in ORAS is fine).
-OPEN the working model. Nothing will render - this is fine, the renderer is disabled. You may enable it by clicking the "Show grid" button but it will most definitely crash at the next step if you will.
-MERGE your OBJ replacement. You will be prompted to enter a model name - don't use spaces or special characters and try not to use too long names. GF's naming conventions are - for props: <place:com(=combined)/t##(=town+linear story number)/c##(=city)/r###(=route)/d###(dungeon)/world##(mapname+mapnumber)>_bm(buildmodel)_<modelnamewithoutspaces>. For maps: <mapname/c##/t##/d##/r###>_ChunkLocX_ChunkLocY.
-SAVE your model. Click on the format dropdown and select Binary Ctr H3D.
  
Of course, there is no support provided. I don't code C# so this is as far as this will get, unless someone PRs stuff or something. Keep in mind though that if you want to contribute to this project, a better idea is probably to start your own, this is a hacky mess and should just be forgotten probably.

ESPICA is a SPICA CLI made for converting models and injecting textures through CTRMap. It is meant to be used in CTRMap only but should work standalone. To invoke the usage guide, run the executable without arguments.
