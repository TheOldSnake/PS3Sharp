using System.Collections.Immutable;

namespace PS3Sharp.Cell
{
    internal static class ErrorCode
    {
        // Used as return values

        // These values are based on RPCS3's source code

        internal const uint CellOk = 0x00000000; // OK
        internal const uint CellCancel = 0x00000001; // Cancel

        internal const uint CellEAgain = 0x80010001; // The resource is temporarily unavailable
        internal const uint CellEInval = 0x80010002; // An invalid argument value is specified
        internal const uint CellENoSys = 0x80010003; // The feature is not yet implemented
        internal const uint CellENoMem = 0x80010004; // Memory allocation failure
        internal const uint CellESrch = 0x80010005; // The resource with the specified identifier does not exist
        internal const uint CellENoEnt = 0x80010006; // The file does not exist
        internal const uint CellENoExec = 0x80010007; // The file is in unrecognized format
        internal const uint CellEDeadlk = 0x80010008; // Resource deadlock is avoided
        internal const uint CellEPerm = 0x80010009; // The operation is not permitted
        internal const uint CellEBusy = 0x8001000A; // The device or resource is busy
        internal const uint CellETimedout = 0x8001000B; // The operation is timed out
        internal const uint CellEAbort = 0x8001000C; // The operation is aborted
        internal const uint CellEFault = 0x8001000D; // Invalid memory access
        internal const uint CellENoChild = 0x8001000E; // Process has no child(s)
        internal const uint CellEStat = 0x8001000F; // State of the target thread is invalid
        internal const uint CellEAlign = 0x80010010; // Alignment is invalid.
        internal const uint CellEKResource = 0x80010011; // Shortage of the kernel resources
        internal const uint CellEIsDir = 0x80010012; // The file is a directory
        internal const uint CellECanceled = 0x80010013; // Operation canceled
        internal const uint CellEExist = 0x80010014; // Entry already exists
        internal const uint CellEIsConn = 0x80010015; // Port is already connected
        internal const uint CellENotConn = 0x80010016; // Port is not connected
        internal const uint CellEAuthFail = 0x80010017; // Program authentication fail
        internal const uint CellENotMself = 0x80010018; // The file is not a MSELF
        internal const uint CellESysVer = 0x80010019; // System version error
        internal const uint CellEAuthFatal = 0x8001001A; // Fatal system error
        internal const uint CellEDom = 0x8001001B; // Math domain violation
        internal const uint CellERange = 0x8001001C; // Math range violation
        internal const uint CellEIlSeq = 0x8001001D; // Illegal multi-byte sequence in input
        internal const uint CellEFPos = 0x8001001E; // File position error
        internal const uint CellEIntr = 0x8001001F; // Syscall was interrupted
        internal const uint CellEFBig = 0x80010020; // File too large
        internal const uint CellEMLink = 0x80010021; // Too many links
        internal const uint CellENFile = 0x80010022; // File table overflow
        internal const uint CellENoSpc = 0x80010023; // No space left on device
        internal const uint CellENoTty = 0x80010024; // Not a TTY
        internal const uint CellEPipe = 0x80010025; // Broken pipe
        internal const uint CellERofs = 0x80010026; // Read-only filesystem (write fail)
        internal const uint CellESPipe = 0x80010027; // Illegal seek (e.g. seek on pipe)
        internal const uint CellE2Big = 0x80010028; // Arg list too long
        internal const uint CellEAcces = 0x80010029; // Access violation
        internal const uint CellEBadF = 0x8001002A; // Invalid file descriptor
        internal const uint CellEIO = 0x8001002B; // Filesystem mounting failed (actually IO error...EIO)
        internal const uint CellEMFile = 0x8001002C; // Too many files open
        internal const uint CellENoDev = 0x8001002D; // No device
        internal const uint CellENotDir = 0x8001002E; // Not a directory
        internal const uint CellENxIo = 0x8001002F; // No such device or IO
        internal const uint CellEXDev = 0x80010030; // Cross-device link error
        internal const uint CellEBadMsg = 0x80010031; // Bad Message
        internal const uint CellEInProgress = 0x80010032; // In progress
        internal const uint CellEMsgSize = 0x80010033; // Message size error
        internal const uint CellENameTooLong = 0x80010034; // Name too long
        internal const uint CellENoLck = 0x80010035; // No lock
        internal const uint CellENotEmpty = 0x80010036; // Not empty
        internal const uint CellENotSup = 0x80010037; // Not supported
        internal const uint CellEFsSpecific = 0x80010038; // File-system specific error
        internal const uint CellEOverFlow = 0x80010039; // Overflow occured
        internal const uint CellENotMounted = 0x8001003A; // Filesystem not mounted
        internal const uint CellENotSData = 0x8001003B; // Not SData
        internal const uint CellESdkVer = 0x8001003C; // Incorrect version in sys_load_param
        internal const uint CellENoLicDisc = 0x8001003D; // Pointer is null. Similar than 0x8001003E but with some PARAM.SFO parameter (TITLE_ID?) embedded.
        internal const uint CellENoLicEnt = 0x8001003E; // Pointer is null

        internal const uint CellSysmoduleErrorUnknown = 0x80012002;

        internal static readonly ImmutableDictionary<ulong, string> CellErrorNames = new Dictionary<ulong, string>()
        {
            [CellOk] = "CELL_OK",
            [CellCancel] = "CELL_CANCEL",

            [CellEAgain] = "CELL_EAGAIN",
            [CellEInval] = "CELL_EINVAL",
            [CellENoSys] = "CELL_ENOSYS",
            [CellENoMem] = "CELL_ENOMEM",
            [CellESrch] = "CELL_ESRCH",
            [CellENoEnt] = "CELL_ENOENT",
            [CellENoExec] = "CELL_ENOEXEC",
            [CellEDeadlk] = "CELL_EDEADLK",
            [CellEPerm] = "CELL_EPERM",
            [CellEBusy] = "CELL_EBUSY",
            [CellETimedout] = "CELL_ETIMEDOUT",
            [CellEAbort] = "CELL_EABORT",
            [CellEFault] = "CELL_EFAULT",
            [CellENoChild] = "CELL_ENOCHILD",
            [CellEStat] = "CELL_ESTAT",
            [CellEAlign] = "CELL_EALIGN",
            [CellEKResource] = "CELL_EKRESOURCE",
            [CellEIsDir] = "CELL_EISDIR",
            [CellECanceled] = "CELL_ECANCELED",
            [CellEExist] = "CELL_EEXIST",
            [CellEIsConn] = "CELL_EISCONN",
            [CellENotConn] = "CELL_ENOTCONN",
            [CellEAuthFail] = "CELL_EAUTHFAIL",
            [CellENotMself] = "CELL_ENOTMSELF",
            [CellESysVer] = "CELL_ESYSVER",
            [CellEAuthFatal] = "CELL_EAUTHFATAL",
            [CellEDom] = "CELL_EDOM",
            [CellERange] = "CELL_ERANGE",
            [CellEIlSeq] = "CELL_EILSEQ",
            [CellEFPos] = "CELL_EFPOS",
            [CellEIntr] = "CELL_EINTR",
            [CellEFBig] = "CELL_EFBIG",
            [CellEMLink] = "CELL_EMLINK",
            [CellENFile] = "CELL_ENFILE",
            [CellENoSpc] = "CELL_ENOSPC",
            [CellENoTty] = "CELL_ENOTTY",
            [CellEPipe] = "CELL_EPIPE",
            [CellERofs] = "CELL_EROFS",
            [CellESPipe] = "CELL_ESPIPE",
            [CellE2Big] = "CELL_E2BIG",
            [CellEAcces] = "CELL_EACCES",
            [CellEBadF] = "CELL_EBADF",
            [CellEIO] = "CELL_EIO",
            [CellEMFile] = "CELL_EMFILE",
            [CellENoDev] = "CELL_ENODEV",
            [CellENotDir] = "CELL_ENOTDIR",
            [CellENxIo] = "CELL_ENXIO",
            [CellEXDev] = "CELL_EXDEV",
            [CellEBadMsg] = "CELL_EBADMSG",
            [CellEInProgress] = "CELL_EINPROGRESS",
            [CellEMsgSize] = "CELL_EMSGSIZE",
            [CellENameTooLong] = "CELL_ENAMETOOLONG",
            [CellENoLck] = "CELL_ENOLCK",
            [CellENotEmpty] = "CELL_ENOTEMPTY",
            [CellENotSup] = "CELL_ENOTSUP",
            [CellEFsSpecific] = "CELL_EFSSPECIFIC",
            [CellEOverFlow] = "CELL_EOVERFLOW",
            [CellENotMounted] = "CELL_ENOTMOUNTED",
            [CellENotSData] = "CELL_ENOTSDATA",
            [CellESdkVer] = "CELL_ESDKVER",
            [CellENoLicDisc] = "CELL_ENOLICDISC",
            [CellENoLicEnt] = "CELL_ENOLICENT",
        }.ToImmutableDictionary();
    }
}
