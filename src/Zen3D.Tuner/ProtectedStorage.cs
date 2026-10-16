using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Zen3D.Tuner;

[SupportedOSPlatform("windows")]
internal static class ProtectedStorage
{
    private static readonly SecurityIdentifier Admins = new(WellKnownSidType.BuiltinAdministratorsSid, null);
    private static readonly SecurityIdentifier SystemSid = new(WellKnownSidType.LocalSystemSid, null);
    private static readonly SecurityIdentifier Users = new(WellKnownSidType.BuiltinUsersSid, null);
    private const FileSystemRights WriteRights = FileSystemRights.Write | FileSystemRights.Delete | FileSystemRights.DeleteSubdirectoriesAndFiles | FileSystemRights.ChangePermissions | FileSystemRights.TakeOwnership;

    public static string EnsureDirectory()
    {
        string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Zen3DTuner");
        RejectReparsePoints(Path.GetDirectoryName(root)!);
        if (!Directory.Exists(root))
        {
            var acl = new DirectorySecurity();
            acl.SetAccessRuleProtection(true, false);
            acl.SetOwner(Admins);
            foreach (var sid in new[] { Admins, SystemSid })
                acl.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            acl.AddAccessRule(new FileSystemAccessRule(Users, FileSystemRights.ReadAndExecute, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            new DirectoryInfo(root).Create(acl);
        }
        AssertProtected(root);
        return root;
    }

    public static void AssertProtected(string path)
    {
        RejectReparsePoints(path);
        FileSystemSecurity acl = Directory.Exists(path)
            ? new DirectoryInfo(path).GetAccessControl()
            : new FileInfo(path).GetAccessControl();
        var owner = acl.GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier
            ?? throw new UnauthorizedAccessException("File owner is unavailable.");
        if (!owner.Equals(Admins) && !owner.Equals(SystemSid))
            throw new UnauthorizedAccessException($"Administrator-owned installation/storage required: {path}");
        foreach (FileSystemAccessRule rule in acl.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            if (rule.AccessControlType == AccessControlType.Allow && (rule.FileSystemRights & WriteRights) != 0 &&
                !rule.IdentityReference.Equals(Admins) && !rule.IdentityReference.Equals(SystemSid))
                throw new UnauthorizedAccessException($"User-writable installation/storage is not allowed for elevated startup: {path}");
    }

    public static void RejectReparsePoints(string path)
    {
        for (var item = new DirectoryInfo(Path.GetFullPath(path)); item is not null; item = item.Parent)
            if ((File.GetAttributes(item.FullName) & FileAttributes.ReparsePoint) != 0)
                throw new UnauthorizedAccessException("Links/junctions are not allowed in elevated storage or installation paths.");
    }

    public static void ProtectNewFile(string path)
    {
        RejectReparsePoints(path);
        var info = new FileInfo(path);
        var acl = info.GetAccessControl();
        acl.SetOwner(Admins);
        acl.SetAccessRuleProtection(true, false);
        foreach (var sid in new[] { Admins, SystemSid })
            acl.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, AccessControlType.Allow));
        acl.AddAccessRule(new FileSystemAccessRule(Users, FileSystemRights.ReadAndExecute, AccessControlType.Allow));
        info.SetAccessControl(acl);
    }

    public static string UserSid
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity.User?.Value ?? throw new UnauthorizedAccessException("Current Windows identity unavailable.");
        }
    }
    public static string StableProfilePath => Path.Combine(EnsureDirectory(), $"stable-{UserSid}.json");
    public static string LogPath => Path.Combine(EnsureDirectory(), $"apply-{UserSid}.log");
}
