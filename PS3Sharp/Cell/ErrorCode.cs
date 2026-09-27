using PS3Sharp.OS.Modules;

namespace PS3Sharp.Cell
{
    public static class ErrorCode
    {
        // Used as return values

        // These values are based on RPCS3's source code

        public const uint CELL_OK = 0x00000000; // OK
        public const uint CELL_CANCEL = 0x00000001; // Cancel

        public const uint CELL_EAGAIN = 0x80010001; // The resource is temporarily unavailable
        public const uint CELL_EINVAL = 0x80010002; // An invalid argument value is specified
        public const uint CELL_ENOSYS = 0x80010003; // The feature is not yet implemented
        public const uint CELL_ENOMEM = 0x80010004; // Memory allocation failure
        public const uint CELL_ESRCH = 0x80010005; // The resource with the specified identifier does not exist
        public const uint CELL_ENOENT = 0x80010006; // The file does not exist
        public const uint CELL_ENOEXEC = 0x80010007; // The file is in unrecognized format
        public const uint CELL_EDEADLK = 0x80010008; // Resource deadlock is avoided
        public const uint CELL_EPERM = 0x80010009; // The operation is not permitted
        public const uint CELL_EBUSY = 0x8001000A; // The device or resource is busy
        public const uint CELL_ETIMEDOUT = 0x8001000B; // The operation is timed out
        public const uint CELL_EABORT = 0x8001000C; // The operation is aborted
        public const uint CELL_EFAULT = 0x8001000D; // Invalid memory access
        public const uint CELL_ENOCHILD = 0x8001000E; // Process has no child(s)
        public const uint CELL_ESTAT = 0x8001000F; // State of the target thread is invalid
        public const uint CELL_EALIGN = 0x80010010; // Alignment is invalid.
        public const uint CELL_EKRESOURCE = 0x80010011; // Shortage of the kernel resources
        public const uint CELL_EISDIR = 0x80010012; // The file is a directory
        public const uint CELL_ECANCELED = 0x80010013; // Operation canceled
        public const uint CELL_EEXIST = 0x80010014; // Entry already exists
        public const uint CELL_EISCONN = 0x80010015; // Port is already connected
        public const uint CELL_ENOTCONN = 0x80010016; // Port is not connected
        public const uint CELL_EAUTHFAIL = 0x80010017; // Program authentication fail
        public const uint CELL_ENOTMSELF = 0x80010018; // The file is not a MSELF
        public const uint CELL_ESYSVER = 0x80010019; // System version error
        public const uint CELL_EAUTHFATAL = 0x8001001A; // Fatal system error
        public const uint CELL_EDOM = 0x8001001B; // Math domain violation
        public const uint CELL_ERANGE = 0x8001001C; // Math range violation
        public const uint CELL_EILSEQ = 0x8001001D; // Illegal multi-byte sequence in input
        public const uint CELL_EFPOS = 0x8001001E; // File position error
        public const uint CELL_EINTR = 0x8001001F; // Syscall was interrupted
        public const uint CELL_EFBIG = 0x80010020; // File too large
        public const uint CELL_EMLINK = 0x80010021; // Too many links
        public const uint CELL_ENFILE = 0x80010022; // File table overflow
        public const uint CELL_ENOSPC = 0x80010023; // No space left on device
        public const uint CELL_ENOTTY = 0x80010024; // Not a TTY
        public const uint CELL_EPIPE = 0x80010025; // Broken pipe
        public const uint CELL_EROFS = 0x80010026; // Read-only filesystem (write fail)
        public const uint CELL_ESPIPE = 0x80010027; // Illegal seek (e.g. seek on pipe)
        public const uint CELL_E2BIG = 0x80010028; // Arg list too long
        public const uint CELL_EACCES = 0x80010029; // Access violation
        public const uint CELL_EBADF = 0x8001002A; // Invalid file descriptor
        public const uint CELL_EIO = 0x8001002B; // Filesystem mounting failed (actually IO error...EIO)
        public const uint CELL_EMFILE = 0x8001002C; // Too many files open
        public const uint CELL_ENODEV = 0x8001002D; // No device
        public const uint CELL_ENOTDIR = 0x8001002E; // Not a directory
        public const uint CELL_ENXIO = 0x8001002F; // No such device or IO
        public const uint CELL_EXDEV = 0x80010030; // Cross-device link error
        public const uint CELL_EBADMSG = 0x80010031; // Bad Message
        public const uint CELL_EINPROGRESS = 0x80010032; // In progress
        public const uint CELL_EMSGSIZE = 0x80010033; // Message size error
        public const uint CELL_ENAMETOOLONG = 0x80010034; // Name too long
        public const uint CELL_ENOLCK = 0x80010035; // No lock
        public const uint CELL_ENOTEMPTY = 0x80010036; // Not empty
        public const uint CELL_ENOTSUP = 0x80010037; // Not supported
        public const uint CELL_EFSSPECIFIC = 0x80010038; // File-system specific error
        public const uint CELL_EOVERFLOW = 0x80010039; // Overflow occured
        public const uint CELL_ENOTMOUNTED = 0x8001003A; // Filesystem not mounted
        public const uint CELL_ENOTSDATA = 0x8001003B; // Not SData
        public const uint CELL_ESDKVER = 0x8001003C; // Incorrect version in sys_load_param
        public const uint CELL_ENOLICDISC = 0x8001003D; // Pointer is null. Similar than 0x8001003E but with some PARAM.SFO parameter (TITLE_ID?) embedded.
        public const uint CELL_ENOLICENT = 0x8001003E; // Pointer is null

        public const uint CELL_SYSMODULE_ERROR_UNKNOWN = 0x80012002;


        public static readonly Dictionary<ulong, string> CellErrorNames = new() {
            { CELL_OK, "CELL_OK" },
            { CELL_CANCEL, "CELL_CANCEL" },

            { CELL_EAGAIN, "CELL_EAGAIN" },
            { CELL_EINVAL, "CELL_EINVAL" },
            { CELL_ENOSYS, "CELL_ENOSYS" },
            { CELL_ENOMEM, "CELL_ENOMEM" },
            { CELL_ESRCH, "CELL_ESRCH" },
            { CELL_ENOENT, "CELL_ENOENT" },
            { CELL_ENOEXEC, "CELL_ENOEXEC" },
            { CELL_EDEADLK, "CELL_EDEADLK" },
            { CELL_EPERM, "CELL_EPERM" },
            { CELL_EBUSY, "CELL_EBUSY" },
            { CELL_ETIMEDOUT, "CELL_ETIMEDOUT" },
            { CELL_EABORT, "CELL_EABORT" },
            { CELL_EFAULT, "CELL_EFAULT" },
            { CELL_ENOCHILD, "CELL_ENOCHILD" },
            { CELL_ESTAT, "CELL_ESTAT" },
            { CELL_EALIGN, "CELL_EALIGN" },
            { CELL_EKRESOURCE, "CELL_EKRESOURCE" },
            { CELL_EISDIR, "CELL_EISDIR" },
            { CELL_ECANCELED, "CELL_ECANCELED" },
            { CELL_EEXIST, "CELL_EEXIST" },
            { CELL_EISCONN, "CELL_EISCONN" },
            { CELL_ENOTCONN, "CELL_ENOTCONN" },
            { CELL_EAUTHFAIL, "CELL_EAUTHFAIL" },
            { CELL_ENOTMSELF, "CELL_ENOTMSELF" },
            { CELL_ESYSVER, "CELL_ESYSVER" },
            { CELL_EAUTHFATAL, "CELL_EAUTHFATAL" },
            { CELL_EDOM, "CELL_EDOM" },
            { CELL_ERANGE, "CELL_ERANGE" },
            { CELL_EILSEQ, "CELL_EILSEQ" },
            { CELL_EFPOS, "CELL_EFPOS" },
            { CELL_EINTR, "CELL_EINTR" },
            { CELL_EFBIG, "CELL_EFBIG" },
            { CELL_EMLINK, "CELL_EMLINK" },
            { CELL_ENFILE, "CELL_ENFILE" },
            { CELL_ENOSPC, "CELL_ENOSPC" },
            { CELL_ENOTTY, "CELL_ENOTTY" },
            { CELL_EPIPE, "CELL_EPIPE" },
            { CELL_EROFS, "CELL_EROFS" },
            { CELL_ESPIPE, "CELL_ESPIPE" },
            { CELL_E2BIG, "CELL_E2BIG" },
            { CELL_EACCES, "CELL_EACCES" },
            { CELL_EBADF, "CELL_EBADF" },
            { CELL_EIO, "CELL_EIO" },
            { CELL_EMFILE, "CELL_EMFILE" },
            { CELL_ENODEV, "CELL_ENODEV" },
            { CELL_ENOTDIR, "CELL_ENOTDIR" },
            { CELL_ENXIO, "CELL_ENXIO" },
            { CELL_EXDEV, "CELL_EXDEV" },
            { CELL_EBADMSG, "CELL_EBADMSG" },
            { CELL_EINPROGRESS, "CELL_EINPROGRESS" },
            { CELL_EMSGSIZE, "CELL_EMSGSIZE" },
            { CELL_ENAMETOOLONG, "CELL_ENAMETOOLONG" },
            { CELL_ENOLCK, "CELL_ENOLCK" },
            { CELL_ENOTEMPTY, "CELL_ENOTEMPTY" },
            { CELL_ENOTSUP, "CELL_ENOTSUP" },
            { CELL_EFSSPECIFIC, "CELL_EFSSPECIFIC" },
            { CELL_EOVERFLOW, "CELL_EOVERFLOW" },
            { CELL_ENOTMOUNTED, "CELL_ENOTMOUNTED" },
            { CELL_ENOTSDATA, "CELL_ENOTSDATA" },
            { CELL_ESDKVER, "CELL_ESDKVER" },
            { CELL_ENOLICDISC, "CELL_ENOLICDISC" },
            { CELL_ENOLICENT, "CELL_ENOLICENT" },
        };
    }
}
