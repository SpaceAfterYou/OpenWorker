﻿// <auto-generated />
using System;
using ow.Framework.Database.Characters;
using ow.Framework.Database.Storages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Migration.Contexts;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace SetupDatabase.Migrations
{
    [DbContext(typeof(MigrationContext))]
    [Migration("20201203225803_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityByDefaultColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.0");

            modelBuilder.Entity("ow.Framework.Database.AccouintPosts.AccountPostModel", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);

                    b.Property<long>("AccountId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("AccountId");

                    b.ToTable("account_posts");
                });

            modelBuilder.Entity("ow.Framework.Database.Accounts.AccountModel", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);

                    b.Property<int>("LastSelectedCharacter")
                        .HasColumnType("integer");

                    b.Property<string>("Mac")
                        .IsRequired()
                        .HasColumnType("CHAR(18)");

                    b.Property<string>("Nickname")
                        .IsRequired()
                        .HasMaxLength(24)
                        .HasColumnType("character varying(24)");

                    b.Property<byte[]>("Password")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.Property<decimal>("SessionKey")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("SoulCash")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("Nickname")
                        .IsUnique();

                    b.HasIndex("SessionKey");

                    b.ToTable("accounts");

                    b.HasData(
                        new
                        {
                            Id = 1L,
                            LastSelectedCharacter = -1,
                            Mac = "00-00-00-00-00-00",
                            Nickname = "sawich",
                            Password = new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 },
                            SessionKey = 0m,
                            SoulCash = 0m
                        },
                        new
                        {
                            Id = 2L,
                            LastSelectedCharacter = -1,
                            Mac = "00-00-00-00-00-00",
                            Nickname = "Leeroy",
                            Password = new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 },
                            SessionKey = 0m,
                            SoulCash = 0m
                        },
                        new
                        {
                            Id = 3L,
                            LastSelectedCharacter = -1,
                            Mac = "00-00-00-00-00-00",
                            Nickname = "Tweekly",
                            Password = new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 },
                            SessionKey = 0m,
                            SoulCash = 0m
                        },
                        new
                        {
                            Id = 4L,
                            LastSelectedCharacter = -1,
                            Mac = "00-00-00-00-00-00",
                            Nickname = "Chelsea",
                            Password = new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 },
                            SessionKey = 0m,
                            SoulCash = 0m
                        },
                        new
                        {
                            Id = 5L,
                            LastSelectedCharacter = -1,
                            Mac = "00-00-00-00-00-00",
                            Nickname = "Dez",
                            Password = new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 },
                            SessionKey = 0m,
                            SoulCash = 0m
                        },
                        new
                        {
                            Id = 6L,
                            LastSelectedCharacter = -1,
                            Mac = "00-00-00-00-00-00",
                            Nickname = "Godo",
                            Password = new byte[] { 85, 165, 86, 15, 55, 213, 63, 202, 45, 212, 222, 16, 50, 189, 195, 225, 180, 162, 114, 124, 92, 175, 112, 216, 10, 215, 203, 48, 247, 21, 175, 175, 228, 21, 179, 235, 218, 60, 112, 218, 130, 134, 115, 78, 84, 169, 234, 143, 2, 230, 79, 161, 26, 114, 50, 30, 54, 112, 179, 90, 221, 191, 60, 250 },
                            SessionKey = 0m,
                            SoulCash = 0m
                        });
                });

            modelBuilder.Entity("ow.Framework.Database.CharacterPosts.CharacterPostModel", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);

                    b.Property<long>("CharacterId")
                        .HasColumnType("bigint");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("CharacterId");

                    b.ToTable("character_posts");
                });

            modelBuilder.Entity("ow.Framework.Database.Characters.CharacterModel", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);

                    b.Property<long>("AccountId")
                        .HasColumnType("bigint");

                    b.Property<byte>("Advancement")
                        .HasColumnType("smallint");

                    b.Property<ApperanceModel>("Appearance")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<BankModel>("Bank")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<EnergyModel>("Energy")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<decimal>("Exp")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("GateId")
                        .HasColumnType("integer");

                    b.Property<long[]>("Gesture")
                        .IsRequired()
                        .HasColumnType("bigint[]");

                    b.Property<int>("Hero")
                        .HasColumnType("integer");

                    b.Property<InventoryModel>("Inventory")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<long[]>("LearnedSkill")
                        .IsRequired()
                        .HasColumnType("bigint[]");

                    b.Property<byte>("Level")
                        .HasColumnType("smallint");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<PlaceModel>("Place")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<long>("PortraitId")
                        .HasColumnType("bigint");

                    b.Property<ProfileModel>("Profile")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<long[]>("QuickSlot")
                        .IsRequired()
                        .HasColumnType("bigint[]");

                    b.Property<long[]>("SkillSlot")
                        .IsRequired()
                        .HasColumnType("bigint[]");

                    b.Property<byte>("SlotId")
                        .HasColumnType("smallint");

                    b.Property<StorageModel[]>("Storage")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<TitleModel>("Title")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.HasKey("Id");

                    b.HasIndex("AccountId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("characters");
                });

            modelBuilder.Entity("ow.Framework.Database.Guilds.GuildModel", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(1024)
                        .HasColumnType("character varying(1024)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(12)
                        .HasColumnType("character varying(12)");

                    b.HasKey("Id");

                    b.ToTable("guilds");
                });

            modelBuilder.Entity("ow.Framework.Database.Storages.ItemModel", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.None);

                    b.Property<string>("BroochSlots")
                        .IsRequired()
                        .HasColumnType("char(15)");

                    b.Property<long>("CharacterId")
                        .HasColumnType("bigint");

                    b.Property<long>("Color")
                        .HasColumnType("bigint");

                    b.Property<int>("Count")
                        .HasColumnType("integer");

                    b.Property<byte>("Durability")
                        .HasColumnType("smallint");

                    b.Property<long>("DyeColor")
                        .HasColumnType("bigint");

                    b.Property<long>("PrototypeId")
                        .HasColumnType("bigint");

                    b.Property<byte>("Quantly")
                        .HasColumnType("smallint");

                    b.Property<int>("SlotId")
                        .HasColumnType("integer");

                    b.Property<byte>("Slots")
                        .HasColumnType("smallint");

                    b.Property<StatModel[]>("Stats")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<int>("StorageType")
                        .HasColumnType("integer");

                    b.Property<long>("TagId")
                        .HasColumnType("bigint");

                    b.Property<UpgradeModel>("Upgrade")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.HasKey("Id");

                    b.HasIndex("CharacterId");

                    b.ToTable("items");
                });

            modelBuilder.Entity("ow.Framework.Database.AccouintPosts.AccountPostModel", b =>
                {
                    b.HasOne("ow.Framework.Database.Accounts.AccountModel", "Account")
                        .WithMany()
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Account");
                });

            modelBuilder.Entity("ow.Framework.Database.CharacterPosts.CharacterPostModel", b =>
                {
                    b.HasOne("ow.Framework.Database.Characters.CharacterModel", "Character")
                        .WithMany()
                        .HasForeignKey("CharacterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Character");
                });

            modelBuilder.Entity("ow.Framework.Database.Characters.CharacterModel", b =>
                {
                    b.HasOne("ow.Framework.Database.Accounts.AccountModel", "Account")
                        .WithMany()
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Account");
                });

            modelBuilder.Entity("ow.Framework.Database.Storages.ItemModel", b =>
                {
                    b.HasOne("ow.Framework.Database.Characters.CharacterModel", "Character")
                        .WithMany()
                        .HasForeignKey("CharacterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Character");
                });
#pragma warning restore 612, 618
        }
    }
}
