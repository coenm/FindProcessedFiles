﻿using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Core.Test")]
[assembly: InternalsVisibleTo("EagleEye.Core.Test")]

[assembly: InternalsVisibleTo("FileImporter")]
[assembly: InternalsVisibleTo("EagleEye.FileImporter")]

// required for FakeItEasy
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
