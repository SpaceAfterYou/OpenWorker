using System.Collections.ObjectModel;
using System.Diagnostics;
using Arch.Core;
using OpenWorker.Hotspot.Modules.Items.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Modules.Items;

public sealed class StorageFactory(Arch.Core.World world)
{
    private StorageArchetype[] Archetypes { get; } =
    [
        new()
        {
            Group = StorageGroup.ShapeEquip,
            Size = ItemModuleDefines.EquippedItemCount,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent)
                /*, typeof(StorageGradeComponent)*/
            ]
        },

        new()
        {
            Group = StorageGroup.AbilityEquip,
            Size = 2,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent)
                /*, typeof(StorageGradeComponent)*/
            ]
        },

        new()
        {
            Group = StorageGroup.Common,
            Size = 24,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent),
                typeof(StorageGradeComponent)
            ]
        },

        new()
        {
            Group = StorageGroup.LookEquip,
            Size = ItemModuleDefines.EquippedItemCount,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent)
                /*, typeof(StorageGradeComponent)*/
            ]
        },

        new()
        {
            Group = StorageGroup.Costume,
            Size = 48,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent),
                typeof(StorageGradeComponent)
            ]
        },

        new()
        {
            Group = StorageGroup.CommonStorage,
            Size = 24,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent),
                typeof(StorageGradeComponent)
            ]
        },

        new()
        {
            Group = StorageGroup.CostumeStorage,
            Size = 48,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent),
                typeof(StorageGradeComponent)
            ]
        },

        new()
        {
            Group = StorageGroup.RepurchaseList,
            Size = 0,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent)
                /*, typeof(StorageGradeComponent)*/
            ]
        },

        new()
        {
            Group = StorageGroup.Cube,
            Size = 384,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent)
                /*, typeof(StorageGradeComponent)*/
            ]
        },

        new()
        {
            Group = StorageGroup.Appearance,
            Size = 0,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent)
                /*, typeof(StorageGradeComponent)*/
            ]
        },

        new()
        {
            Group = StorageGroup.Cash,
            Size = 384,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent)
                /*, typeof(StorageGradeComponent)*/
            ]
        },

        new()
        {
            Group = StorageGroup.CashStorage,
            Size = 384,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent)
                /*, typeof(StorageGradeComponent)*/
            ]
        },

        new()
        {
            Group = StorageGroup.League,
            Size = 0,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent)
                /*, typeof(StorageGradeComponent)*/
            ]
        },

        new()
        {
            Group = StorageGroup.AccountCommonStorage,
            Size = 0,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent),
                typeof(StorageGradeComponent)
            ]
        },

        new()
        {
            Group = StorageGroup.AccountFashionStorage,
            Size = 0,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent),
                typeof(StorageGradeComponent)
            ]
        },

        new()
        {
            Group = StorageGroup.AccountCashStorage,
            Size = 0,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent)
                /*, typeof(StorageGradeComponent)*/
            ]
        },

        new()
        {
            Group = StorageGroup.Post,
            Size = 0,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent)
                /*, typeof(StorageGradeComponent)*/
            ]
        },

        new()
        {
            Group = StorageGroup.ExchangeSell,
            Size = 0,
            Components =
            [
                typeof(StorageGroupComponent),
                typeof(StorageContentComponent)
                /*, typeof(StorageGradeComponent)*/
            ]
        }
    ];

    public Entity Create(StorageGroup group)
    {
        var archetype = Archetypes.First(x => x.Group == group);

        var groupComponent = new StorageGroupComponent(group);
        var contentComponent = new StorageContentComponent(new Entity[archetype.Size]);

        for (var i = 0; i < contentComponent.SlotList.Length; i++)
        {
            contentComponent.SlotList[i] = Entity.Null;
        }

        var storage = world.Create(archetype.Components);

        world.Set(storage, contentComponent, groupComponent);

        return storage;
    }
}