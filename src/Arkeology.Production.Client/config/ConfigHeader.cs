namespace Arkeology.Production.Client;

public record ConfigHeader(byte VersionMajor, byte VersionMinor, long BuildTime, StringTable Strings);
