namespace OpenWorker.Services.Gate.Api.Handlers.HotSpot.Person.ToRefactor;

public static class PersonHandler
{
    //private static CharacterPacketSharedStructure.CostumeInfos.CostumeInfo CreateCostumeInfo(
    //    PersonValue.StoragesInfos.CostumeItems.CostumeItem item) =>
    //    new() { Color = item.Color, PrototypeId = item.PrototypeId };

    //[Handler(CategoryCommand.Character, CharacterCommand.CreateReq, typeof(SyncServer), SyncServerType.Login)]
    //public static async ValueTask OnCreateRequestAsync(SessionBridge session, CharacterCreateRequest request)
    //{
    //if (request.Character.Main.Name.Length > CommonDefines.MaxCharacterNameLength)
    //{
    //    return;
    //}

    //if (request.Character.Main.Name.Length < CommonDefines.MinCharacterNameLength)
    //{
    //    return;
    //}

    //if (request.Character.Main.Class == Class.None || !Enum.IsDefined(typeof(Class), request.Character.Main.Class))
    //{
    //    NetworkUtils.Drop(session);
    //}

    //if (!session.Server.Tables.CustomizeHair.TryGetValue(request.Character.Main.Class, out CustomizeHairEntity? customizeHairEntity))
    //{
    //    NetworkUtils.Drop(session);
    //}

    //if (request.Character.Main.Appearance.Hair.Style == 0 || !customizeHairEntity.Style.Contains(request.Character.Main.Appearance.Hair.Style))
    //{
    //    NetworkUtils.Drop(session);
    //}

    //if (!session.Server.Tables.CustomizeEyes.TryGetValue(request.Character.Main.Class, out CustomizeEyesEntity? customizeEyesEntity))
    //{
    //    NetworkUtils.Drop(session);
    //}

    //if (request.Character.Main.Appearance.EyesColor == 0 || !customizeEyesEntity.Color.Contains(request.Character.Main.Appearance.EyesColor))
    //{
    //    NetworkUtils.Drop(session);
    //}

    //if (!session.Server.Tables.CustomizeSkin.TryGetValue(request.Character.Main.Class, out CustomizeSkinEntity? customizeSkinEntity))
    //{
    //    NetworkUtils.Drop(session);
    //}

    //if (request.Character.Main.Appearance.SkinColor == 0 || !customizeSkinEntity.Color.Contains(request.Character.Main.Appearance.SkinColor))
    //{
    //    NetworkUtils.Drop(session);
    //}

    //if (!session.Server.Tables.CharacterInfo.TryGetValue((ushort)(CharacterCreateHelper.CharacterOffset * (byte)request.Character.Main.Class), out CharacterInfoEntity? characterInfoEntity))
    //{
    //    NetworkUtils.Drop(session);
    //}

    //if (request.Outfit == 0 || characterInfoEntity.DefaultCostumeIds.Contains(request.Outfit))
    //{
    //    NetworkUtils.Drop(session);
    //}

    //if (!session.Server.Tables.ClassSelectInfo.TryGetValue(request.Character.Main.Class, out ClassSelectInfoEntity? classInfoEntity))
    //{
    //    NetworkUtils.Drop(session);
    //}

    //PhotoItemEntity? photoItemEntity = session.Server.Tables.PhotoItem.Values.FirstOrDefault(c => c.Class == request.Character.Main.Class && c.PromotionInfo == 1);
    //if (photoItemEntity is null)
    //{
    //    NetworkUtils.Drop(session);
    //}

    //await AddCharacter(session, request, characterInfoEntity, photoItemEntity);
    //    await SendCharactersListAsync(session);
    //}

    //private static async Task AddCharacter(SessionBridge session, CharacterCreateRequest request, CharacterInfoEntity characterInfoEntity, PhotoItemEntity photoItemEntity)
    //{
    //    await using CharacterContext context = session.Server.DatabaseCharacterFactory.CreateDbContext();

    //    if (await context.Characters.AnyAsync(c => c.Slot == request.Slot && c.AccountId == session.Account.Id))
    //    {
    //        return;
    //    }

    //    if (await context.Characters.AnyAsync(c => c.Name == request.Character.Main.Name))
    //    {
    //        return;
    //    }

    //    CharacterModel model = CharacterCreateHelper.CreateModel(session.Account, request, session.Server.Instance, characterInfoEntity, photoItemEntity);
    //    context.Add(model);

    //    await context.SaveChangesAsync();

    //    session.Characters.LastSelected = await session.Server.CreateEmptyCharacaterAsync(model);
    //    if (!session.Characters.TryAdd(model.Id, session.Characters.LastSelected))
    //    {
    //        return;
    //    }

    //    return;
    //}



    //[Handler(CategoryCommand.Character, CharacterCommand.ListReq, typeof(SyncServer), SyncServerType.Login)]
    //public static async ValueTask OnListRequestAsync(Entity entity) =>
    //    await SendCharactersListAsync(session);



    //[Handler(CategoryCommand.Character, CharacterCommand.UpdateSpecialOptionList, typeof(SyncServer),
    //    SyncServerType.Login)]
    //public static ValueTask OnUpdateSpecialOptionList() => ValueTask.CompletedTask;
}
