namespace OpenWorker.Hotspot.Modules.Persons.Enums;

public enum ChangeServerType : byte
{
    EnterMap = 0x0,
    ChangeChannel = 0x1,
    ExitMaze = 0x2,
    EnterMazeFromMaze = 0x3,
    EnterDistrict = 0x4,
    EnterDistrict6 = 0x5,
    Login = 0x6
}