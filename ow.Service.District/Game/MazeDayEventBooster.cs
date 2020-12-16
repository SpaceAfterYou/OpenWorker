using ow.Framework.Game.Datas.Bin.Table.Entities;

namespace ow.Service.District.Game
{
    internal sealed record MazeDayEventBooster
    {
        internal ushort Id { get; }
        internal MazeInfoTableEntity Maze { get; }

        internal MazeDayEventBooster(ushort id, MazeInfoTableEntity maze) => (Id, Maze) = (id, maze);
    }
}