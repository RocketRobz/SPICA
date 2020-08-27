# SPICA Embedded Framework
A CLI extension of gdkchan's experimental H3D tool for serializing/deserializing BCH.

Dependencies:
- OpenTK
- OpenTK.GLControl

Both can be found on NuGet.

Note: The version of OpenTK.GLControl on NuGet is broken, so it's recommended to build it yourself from source and manually add a reference to the compiled library.
OpenTK git can be found [here](https://github.com/opentk/opentk).

You will need .NET Framework 4.6 and a GPU capable of OpenGL 3.3 at least.

SPICA can be built on Windows using Visual Studio or Linux/Mac using [Mono](https://www.mono-project.com/).
See `README.mono.md` for details.

# Features of this fork

*In order of non-importance*

- Material Animation playback bugfixes
- Improved support for reading SMD materials
- Changed the scene merging flow to retain compatibility with the revision of the first loaded file (not forced 0x21 as in regular SPICA).
- Ability to remove scene members with a button in the GUI
- Improved format support for generic Game Freak containers
- Small CtrH3D.Model bugfix with huge consequences, such as full support for Pokémon OR/AS overworld map models.
- Hugely expanded OBJ importer with support for alpha blending, render layers, vertex colors (using an extra "vc" OBJ command outside of the common specifications) and safe material assignment.
- Full support of the Common Interchange Format, allowing to convert CTRMap Creative Studio's scene files to H3D.

# Non-features of this fork

- Pokémon X/Y support.
- Warranty that anything you import will work. In fact, most things won't.
- Animation importing. Head over to CTRMap for that.