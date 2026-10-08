namespace PS3Sharp.OS.Modules
{
    internal struct ModuleStubInfo
    {
        //This is not one of the PS3 structs, I made it just to make identifying/patching the module function easier
        internal readonly string ModuleName;
        internal readonly string FunctionName;
        internal readonly uint NID;
        internal readonly uint StubAddress; //The address of the beginning of the block
        internal uint PatchStartAddress;    //The address of the patched code

        internal ModuleStubInfo(string moduleName, string functionName, uint nid, uint stubAddress)
        {
            ModuleName = moduleName;
            FunctionName = functionName;
            NID = nid;
            StubAddress = stubAddress;
        }
    }
}
