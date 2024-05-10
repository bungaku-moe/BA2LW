<h1 align="center">Blue Archive to Live Wallpaper<br/>(BA2LW)</h1>

<div align="center">
  <h1>🚧 Under Development 🚧</h1>
  <p><a href="./README.md">EN</a> | ID</p>
</div>

## Table of Contents

- [Overview](#overview)
- [Wallpaper Data Structures](#wallpaper-data-structures)
- [Configuration Contents](#configuration-contents)
  - [config.json](#configjson)
  - [settings.json](#settingsjson)

## Overview

BA2LW is a project intended to make [Blue Archive](https://bluearchive.nexon.com/ "Visit Blue Archive official website") Memorial Lobby used as Interactive Live Wallpaper on your desktop.

This project is a fork of [ba2wall](https://github.com/Tualin14/ba2wall) by [Tualin14](https://github.com/Tualin14) that no longer maintained. You can checkout his work at [Steam Wallpaper Engine Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2875378435 "Visit Tualin14 Workshop").

## Wallpaper Data Structures

- `BA2LW.exe` Main executable.
- `Data/` Root directory contains all charater L2D assets & settings.
  - `config.json` Global configuration file, such as set the current active wallpaper, FPS, etc.
  - `{Character Name}` Root directory for each character assets & settings.
    - `Voice/` Character `Memorial Lobby` voice assets. _(Customizable)_
    - `*.atlas` Character Spine Atlas asset.
    - `*.png` Character Spine Texture asset.
    - `*.skel` Character Spine Skeleton asset.
    - `settings.json` The settings for this L2D, such as `patRange`, `bones`.
    - `Theme.ogg` The wallpaper background music. _(Customizable)_
  - `{Other Character}`
  - ...

## Configuration Contents

### config.json

- [string] `wallpaper` The current active wallpaper. Based on the character directory name `{Character Name}`.
- [int] `fps` The FPS limit. The value must be absolute number.
- [bool] `debug` Show additional information, such as interactable area, bones name, etc. _(Developer Only)_

### settings.json

- [string] `student` Student file name. Based on Spine assets base name.
  (E.g. `Shiroko_home.atlas`, `Shiroko_home.png`, `Shiroko_home2.png`. The student name would be `Shiroko_home`.)
- [bool] `rotation` Straighten the character. Can be used by student like Xinnai. _(Need more info)_
- [float] `scale` The scale of character & background Spine.
- [float] `lookRange` The max range for the eyes to move.
- [string[]] `imageList` The list of character Spine images.
- `bone`
  - [string] `eyeL` Character left eye root bone name.
  - [string] `eyeR` Character right eye root bone name.
  - [string] `halo` Character halo root bone name.
  - [string] `neck` Character neck root bone name.
- `pat`
  - [float] `range` Pat area of the head. Rectangle shaped.
  - [bool] `somethingWrong` Set this to `true` if the head does not follow the mouse movement when you pat. _(Need more info)_
- `bgm`
  - [bool] `enable` Enable/Disable background music.
  - [float] `volume` Background music volume. Range: 0 ~ 1.
  - [string] `clip` The file name of the background music.
- `sfx` The L2D sound effect. A few L2D have ambient sounds, such as Kayoko.
  - [bool] `enable` Enable/Disable sound effect.
  - [string] `name` The sound effect file name.
  - [float] `volume` Sound effect volume. Range: 0 ~ 1.
- `talk`
  - [string] `voiceData` The directory name of the character voice assets.
  - [float] `volume` Character voice volume. Range: 0 ~ 1.
  - [bool] `onlyTalk` Some student sound events are not specifically set and they are all turned on for Talk events. If there is no sound, set it to `true`.
  - [int] `maxIndex` The number of voice talk animations. Counted only the first number from the last.  
    (E.g. `Hihumi_MemorialLobby_1_1.ogg`, `Hihumi_MemorialLobby_1_2.ogg`, `Hihumi_MemorialLobby_2_1.ogg`. The index are `2`.)
- `bg` If the background is also animated, such as Hoshino & Yuzu.
  - [bool] `isSpine` Is the background animated?
  - [string] `name` Background image file name. The naming rule are same as `student`.
  - `state`
    - [bool] `more` Whether there are other states besides the default state. Such as star field background and animation of a whale. _(Need more info)_
    - [string] `name` other state names. _(Need more info)_
  - [string[]] `imageList` The list of background Spine images.

---

> <h1 align="center">🚧 Need more information 🚧</h1>

1. Open the program with debug to see the display on the left
2. Open the program and the interaction range is correct.
3. Because these files do not follow certain naming conventions.

   Take halo as an example, the general root bone is named Halo, Halo_Root, Halo_01

4. There are cases where the left and right eyes are named oppositely, such as Baizi

## Student Setup Examples

<details>
<summary>Koharu</summary>
<pre>
{
    "student": "Koharu_home",
    "rotation":true,
    "scale":1,
    "imageList": [
        "Koharu_home",
        "Koharu_home2"
    ],
    "bgm": {
        "enable": true,
        "volume": 0.2
    },
    "talk": {
        "volume": 1,
        "onlyTalk": true,
        "maxIndex": 5
    },
    "bone": {
        "eyeL": "L_Eye_1_01",
        "eyeR": "R_Eye_1_01",
        "halo": "Halo_Root",
        "neck": "Neck_01"
    }
}
</pre>
</details>

<details>
<summary>Kayoko (with the sound of rain in the background)</summary>
<pre>
{
    "student": "Kayoko_home",
    "rotation": false,
    "scale": 1,
    "imageList": [
        "Kayoko_home",
        "Kayoko_home2"
    ],
    "bgm": {
        "enable": true,
        "volume": 0.2
    },
    "se": {
        "enable": true,
        "name": "Rain.wav",
        "volume": 0.4
    },
    "talk": {
        "volume": 1,
        "onlyTalk": true,
        "maxIndex": 5
    },
    "bone": {
        "eyeL": "L_Eye_01",
        "eyeR": "R_Eye_01",
        "halo": "Halo_Root",
        "neck": "Neck"
    }
}
</pre>
</details>

<details>
<summary>Hoshino (the background is animated)</summary>
<pre>
{
    "student": "Hoshino_home",
    "rotation": false,
    "scale": 1,
    "imageList": [
        "Hoshino_home"
    ],
    "bgm": {
        "enable": true,
        "volume": 0.2
    },
    "talk": {
        "volume": 1,
        "onlyTalk": false,
        "maxIndex": 3
    },
    "bone": {
        "eyeL": "L_Eye",
        "eyeR": "R_Eye",
        "halo": "Halo_01",
        "neck": "Neck"
    },
    "bg": {
        "isSpine": true,
        "name": "Hoshino_home_background",
        "state": {
            "more": true,
            "name": "WhaleMove_01_R"
        },
        "imageList": [
            "Hoshino_home_background",
            "Hoshino_home_background2"
        ]
    }
}
</pre>
</details>

<details>
<summary>Hifumi (to solve the problem that the touch head does not move with the mouse)</summary>
<pre>
{
    "student": "Hihumi_home",
    "rotation": false,
    "scale": 1,
    "lookRange": 0.5,
    "pat": {
        "range": 0.3,
        "somethingWrong": true
    },
    "imageList": [
        "Hihumi_home",
        "Hihumi_home2"
    ],
    "bgm": {
        "enable": true,
        "volume": 0.3
    },
    "talk": {
        "volume": 1,
        "onlyTalk": false,
        "maxIndex": 6
    },
    "bone": {
        "eyeL": "L_Eye_01",
        "eyeR": "R_Eye_01",
        "halo": "Halo_01",
        "neck": "Neck"
    }
}</pre>
</details>

---

## Credits

- [Tualin14](https://github.com/Tualin14 "Visit Tualin14 GitHub profile"), the original creator.
- [spine-unity](http://en.esotericsoftware.com/spine-unity-download "Visit spine-unity official website"), the backbone of Blue Archive Live 2D engine.
  > You can use [my forked spine-unity](https://github.com/kiraio-moe/spine-unity) that support installing via Package Manager.
- [UniTask](https://github.com/Cysharp/UniTask "Visit UniTask GitHub repo"), the Asynchronous library for faster runtime speed.
- [SerializedDictionary](https://github.com/ayellowpaper/SerializedDictionary "Visit SerializedDictionary GitHub repo"), a handy tool to inspecting Dictionary on Unity Inspector that's not officially supported.


## License

The project is licensed under GNU GPL 3.0, not Blue Archive related assets.

For more information about the GNU General Public License version 3.0 (GNU GPL 3.0), please refer to the official GNU website: <https://www.gnu.org/licenses/gpl-3.0.html>

## Disclaimer

This project is not affiliated with Nexon, NEXON Games Co., Ltd. nor any their affiliator.
It's intended as a tool for fun for Blue Archive Sensei's. Any Blue Archive assets and resources used in this project is property and copyright of those respective authors.

[Blue Archive](https://bluearchive.nexon.com/ "Visit Blue Archive official website") is property dan copyright of Nexon, NEXON Games Co., Ltd.
