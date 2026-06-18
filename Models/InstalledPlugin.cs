using System.Collections.Generic;

namespace VstDeleter.Models;

public record InstalledPlugin(string Name, List<string> Formats);
