using System.Runtime.CompilerServices;

// Data assets are written only by the CSV importer (and tests), never by gameplay code.
[assembly: InternalsVisibleTo("Tactics.Editor")]
[assembly: InternalsVisibleTo("Tactics.Tests.EditMode")]
