using PS3Sharp.Cell.PPU;
using PS3Sharp.OS.NID;
using System.Xml.Linq;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    public static class CellSysModule
    {
        public static ulong CellSysmoduleLoadModule(PPU ppu)
        {
            //TODO
            ushort id = ppu.ReadArg<ushort>(ARG0);

            if (ModuleMap.TryGetValue(id, out string? name))
            {
                Console.WriteLine($"[CellSysModule] cellSysmoduleLoadModule requested: {name}");

            }
            else
            {
                Console.WriteLine($"[CellSysModule] cellSysmoduleLoadModule requested: unknown");
            }

            return CELL_OK;
        }

        public static ulong CellSysmoduleUnloadModule(PPU ppu)
        {
            Console.WriteLine($"[CellSysModule] cellSysmoduleUnloadModule");
            return CELL_OK;
        }

        public static ulong CellSysmoduleIsLoaded(PPU ppu)
        {
            Console.WriteLine($"[CellSysModule] cellSysmoduleIsLoaded");
            return CELL_OK;
        }

        public static readonly Dictionary<ushort, string> ModuleMap = new() {
            { 0x0000, "sys_net" },
            { 0x0001, "cellHttp" },
            { 0x0002, "cellHttpUtil" },
            { 0x0003, "cellSsl" },
            { 0x0004, "cellHttps" },
            { 0x0005, "libvdec" },
            { 0x0006, "cellAdec" },
            { 0x0007, "cellDmux" },
            { 0x0008, "cellVpost" },
            { 0x0009, "cellRtc" },
            { 0x000A, "cellSpurs" },
            { 0x000B, "cellOvis" },
            { 0x000C, "cellSheap" },
            { 0x000D, "cellSync" },
            { 0x000E, "sys_fs" },
            { 0x000F, "cellJpgDec" },
            { 0x0010, "cellGcmSys" },
            { 0x0011, "cellAudio" },
            { 0x0012, "cellPamf" },
            { 0x0013, "cellAtrac" },
            { 0x0014, "cellNetCtl" },
            { 0x0015, "cellSysutil" },
            { 0x0016, "sceNp" },
            { 0x0017, "sys_io" },
            { 0x0018, "cellPngDec" },
            { 0x0019, "cellFont" },
            { 0x001A, "cellFontFT" },
            { 0x001B, "cell_FreeType2" },
            { 0x001C, "cellUsbd" },
            { 0x001D, "cellSail" },
            { 0x001E, "cellL10n" },
            { 0x001F, "cellResc" },
            { 0x0020, "cellDaisy" },
            { 0x0021, "cellKey2char" },
            { 0x0022, "cellMic" },
            { 0x0023, "cellCamera" },
            { 0x0024, "cellVdecMpeg2" },
            { 0x0025, "cellVdecAvc" },
            { 0x0026, "cellAdecLpcm" },
            { 0x0027, "cellAdecAc3" },
            { 0x0028, "cellAdecAtx" },
            { 0x0029, "cellAdecAt3" },
            { 0x002A, "cellDmuxPamf" },
            { 0x002B, "BAD" },
            { 0x002C, "BAD" },
            { 0x002D, "BAD" },
            { 0x002E, "sys_lv2dbg" },
            { 0x002F, "cellSysutilAvcExt" },
            { 0x0030, "cellUsbPspcm" },
            { 0x0031, "cellSysutilAvconfExt" },
            { 0x0032, "cellUserInfo" },
            { 0x0033, "cellSaveData" },
            { 0x0034, "cellSubDisplay" },
            { 0x0035, "cellRec" },
            { 0x0036, "cellVideoExportUtility" },
            { 0x0037, "cellGameExec" },
            { 0x0038, "sceNp2" },
            { 0x0039, "cellSysutilAp" },
            { 0x003A, "sceNpClans" },
            { 0x003B, "cellOskExtUtility" },
            { 0x003C, "cellVdecDivx" },
            { 0x003D, "cellJpgEnc" },
            { 0x003E, "cellGame" },
            { 0x003F, "cellBGDLUtility" },
            { 0x0040, "cell_FreeType2" },
            { 0x0041, "cellVideoUpload" },
            { 0x0042, "cellSysconfExtUtility" },
            { 0x0043, "cellFiber" },
            { 0x0044, "sceNpCommerce2" },
            { 0x0045, "sceNpTus" },
            { 0x0046, "cellVoice" },
            { 0x0047, "cellAdecCelp8" },
            { 0x0048, "cellCelp8Enc" },
            { 0x0049, "cellSysutilMisc" },
            { 0x004A, "cellMusicUtility" },
            { 0x004B, "libad_core" },
            { 0x004C, "libad_async" },
            { 0x004D, "libad_billboard_util" },
            { 0x004E, "cellScreenShotUtility" },
            { 0x004F, "cellMusicDecodeUtility" },
            { 0x0050, "cellSpursJq" },
            { 0x0052, "cellPngEnc" },
            { 0x0053, "cellMusicDecodeUtility" },
            { 0x0054, "libmedi" },
            { 0x0055, "cellSync2" },
            { 0x0056, "sceNpUtil" },
            { 0x0057, "cellRudp" },
            { 0x0059, "sceNpSns" },
            { 0x005A, "libgem" },
            { 0x005C, "cellCrossController" },
            { 0xF00A, "cellCelpEnc" },
            { 0xF010, "cellGifDec" },
            { 0xF019, "cellAdecCelp" },
            { 0xF01B, "cellAdecM2bc" },
            { 0xF01D, "cellAdecM4aac" },
            { 0xF01E, "cellAdecMp3" },
            { 0xF023, "cellImeJpUtility" },
            { 0xF028, "cellMusicUtility" },
            { 0xF029, "cellPhotoUtility" },
            { 0xF02A, "cellPrintUtility" },
            { 0xF02B, "cellPhotoImportUtil" },
            { 0xF02C, "cellMusicExportUtility" },
            { 0xF02E, "cellPhotoDecodeUtil" },
            { 0xF02F, "cellSearchUtility" },
            { 0xF030, "cellSysutilAvc2" },
            { 0xF034, "cellSailRec" },
            { 0xF035, "sceNpTrophy" },
            { 0xF044, "cellSysutilNpEula" },
            { 0xF053, "cellAdecAt3multi" },
            { 0xF054, "cellAtracMulti" }
        };
    }
}
