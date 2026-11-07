using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Persistent;

namespace OpenWorker.Persistence;

public readonly record struct OtherPersonProfile(
    string Name,
    byte Hero,
    int TitlePrimary,
    int TitleSecondary,
    int TitleAbilityPrimary,
    int TitleAbilitySecondary,
    byte CommunityState,
    string CommunityComment,
    string CommunityMemo,
    byte[] OptionList,
    bool IsFriend);

/// <summary>
/// What a person says about themselves. These are saved at once: the others can ask for them at any time.
/// </summary>
public static class PersonProfileStore
{
    public static async Task SaveTitlesAsync(
        this IDbContextFactory<PersistenceContext> factory,
        int person,
        int abilityPrimary,
        int abilitySecondary,
        int primary,
        int secondary)
    {
        await using var database = await factory.CreateDbContextAsync();

        await database.Persons
            .Where(e => e.Id == person)
            .ExecuteUpdateAsync(e => e
                .SetProperty(x => x.TitleAbilityPrimary, abilityPrimary)
                .SetProperty(x => x.TitleAbilitySecondary, abilitySecondary)
                .SetProperty(x => x.TitlePrimary, primary)
                .SetProperty(x => x.TitleSecondary, secondary));
    }

    public static async Task SaveTitleListAsync(this IDbContextFactory<PersistenceContext> factory, int person, int[] titles)
    {
        await using var database = await factory.CreateDbContextAsync();

        await database.Persons
            .Where(e => e.Id == person)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.TitleList, titles));
    }

    public static async Task SaveGesturesAsync(this IDbContextFactory<PersistenceContext> factory, int person, int[] gestures)
    {
        await using var database = await factory.CreateDbContextAsync();

        await database.Persons
            .Where(e => e.Id == person)
            .ExecuteUpdateAsync(e => e.SetProperty(x => x.GestureList, gestures));
    }

    public static async Task SaveCommunityAsync(
        this IDbContextFactory<PersistenceContext> factory,
        int person,
        byte state,
        string comment,
        string memo)
    {
        await using var database = await factory.CreateDbContextAsync();

        await database.Persons
            .Where(e => e.Id == person)
            .ExecuteUpdateAsync(e => e
                .SetProperty(x => x.CommunityState, state)
                .SetProperty(x => x.CommunityComment, comment)
                .SetProperty(x => x.CommunityMemo, memo));
    }

    public static async Task<OtherPersonProfile?> LoadOtherPersonAsync(
        this IDbContextFactory<PersistenceContext> factory,
        int person,
        int viewer)
    {
        await using var database = await factory.CreateDbContextAsync();

        var profile = await database.Persons
            .AsNoTracking()
            .Where(e => e.Id == person)
            .Select(e => new
            {
                e.Name,
                e.Hero,
                e.TitlePrimary,
                e.TitleSecondary,
                e.TitleAbilityPrimary,
                e.TitleAbilitySecondary,
                e.CommunityState,
                e.CommunityComment,
                e.CommunityMemo,
                e.OptionList
            })
            .FirstOrDefaultAsync();

        if (profile is null)
        {
            return null;
        }

        var isFriend = await database.Friends
            .AsNoTracking()
            .AnyAsync(e => e.OwnerId == viewer && e.TargetId == person && e.Kind == FriendKind.Friend);

        return new OtherPersonProfile(
            profile.Name,
            (byte)profile.Hero,
            profile.TitlePrimary,
            profile.TitleSecondary,
            profile.TitleAbilityPrimary,
            profile.TitleAbilitySecondary,
            profile.CommunityState,
            profile.CommunityComment,
            profile.CommunityMemo,
            profile.OptionList,
            isFriend);
    }
}
