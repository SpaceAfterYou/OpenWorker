# OpenWorker

![AppVeyor](https://img.shields.io/appveyor/build/sawich/openworker?style=for-the-badge)
![CodeFactor Grade](https://img.shields.io/codefactor/grade/github/sawich/openworker?style=for-the-badge)
![Lines of code](https://img.shields.io/tokei/lines/github/sawich/openworker?style=for-the-badge)
![asd](https://img.shields.io/badge/Mat-Worker-lightgrey?style=for-the-badge&logo=data:image/webp;base64,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)
![Discord](https://img.shields.io/discord/606442027873206292?style=for-the-badge)
![Twitch Status](https://img.shields.io/twitch/status/sawich?style=for-the-badge)

Just another server emulator for korean SoulWoker written on .NET 5.

### Chat

[Discord channel](http://discord.gg/SequFJP)

# How to

### Database

- Used **PostgreSQL** for storage.
- Used **Redis** for cross-server messaging.

Apply migration with nuget console: `update-database -Context MigrationContext -Project SetupDatabase`

### Configuration

#### Login Service

_appsettings.json_

```json
{
  ...
  // Must match with gate appsettings.json (see below)
  "Gates": [
    {
      "Id": 0,
      "Name": "Ashes of Memories",
      "Host": {
        "Ip": "127.0.0.1",
        "Port": 10010
      }
    },
    {
      "Id": 1,
      "Name": "Ashes of Melodies",
      "Host": {
        "Ip": "127.0.0.1",
        "Port": 10020
      }
    },
    ...
    ...
  ],
  ...
}
```

#### Gate Service

_appsettings.json_

```json
{
  "Id": 0,
  "Name": "Ashes of Memories",
  "Host": {
    "Ip": "127.0.0.1",
    "Port": 10010
  },
  ...
}
```
####
**Another instance**

####

_appsettings.json_

```json
{
  "Id": 1,
  "Name": "Ashes of Melodies",
  "Host": {
    "Ip": "127.0.0.1",
    "Port": 10020
  },
  ...
}
```

### Start

Just build solution and launch output files.

## Services

- ♿ _Login Service_ -- Server, where player select gate and enter credentials;
- 🦀 _Gate Service_ -- Server, where player select/create/delete/etc character;
- 🍟 _World Service_ -- In progress...
