# Blue Archive to Live Wallpaper (BA2LW)

## Overview

This project is intended to restore [Blue Archive](https://bluearchive.nexon.com/ "Visit Blue Archive official website") Memorial Lobby and used it as interactive Live Wallpaper.

This project is a fork of [ba2wall](https://github.com/Tualin14/ba2wall/releases) by [Tualin14](https://github.com/Tualin14) that no longer maintained. You can checkout his(?) work at [Steam Wallpaper Engine Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2875378435 "Visit Tualin14 Workshop").

## Game Data Structure

- `Data/` All required data
  - `Voice/` Voice assets
  - `settings.json` Settings file
  - `Theme.ogg` Background music

## settings.json Contents

- `student` student file name
- `debug` Debug, check the trigger position
- `rotation` Straighten the characters, it can be used by students like Xinnai
- `scale` zoom
- `lookRange` Annotation range, in the shape of a square with sides parallel to the eyes
- `pat`
  - `range` Touch the area of the head, shaped as a line parallel to the eyes
  - `somethingWrong` Set this to true if the touch head does not follow the mouse movement
- `imageList` List of pictures, write as many as there are
- `bgm`
  - `enable` If you want to mute bgm, you can turn it off directly to save a little memory
  - `volume` Volume 0.0~1.0
- `se` Sound effects, a few wallpapers have ambient sounds, such as Kayoko
  - `enable` enable sound
  - `name` sound file name
  - `volume` Volume 0.0~1.0
- `talk`
  - `volume` Volume 0.0~1.0
  - `onlyTalk` Some student sound events are not specifically specified, and they are all turned on for Talk events. If there is no sound, change it to true.
  - `maxIndex` Number of voice animations
- `bone`
  - `eyeL` Root bone name of left eye
  - `eyeR` Right eye root bone name
  - `halo` Aura Root Bone Name
  - `neck` neck bone name
- `bg` If the background is also an animation setting, such as Hoshino, Yuzu
  - `isSpine` Whether the background is also animated
  - `name` background image name
  - `state`
    - `more` Whether there are other states besides the default state. Such as star field background and animation of whale movement
    - `name` other state names
  - `imageList` List of background images, write as many as there are

---

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
    "debug": false,
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
    "debug": false,
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
    "debug": true,
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
    "debug": true,
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

## Credits

- [spine-unity](http://en.esotericsoftware.com/spine-unity-download "Visit spine-unity official website")

## License

This project is licensed under GNU GPL 3.0.

For more information about the GNU General Public License version 3.0 (GNU GPL 3.0), please refer to the official GNU website: <https://www.gnu.org/licenses/gpl-3.0.html>

## Disclaimer

This project is not affiliated with Nexon, NEXON Games Co., Ltd. nor any their affiliator.

This project is intended only as a tool for fun. Any game assets and resources related to Blue Archive used in this project is property and copyright of those respective authors.

[Blue Archive](https://bluearchive.nexon.com/ "Visit Blue Archive official website") is property dan copyright of Nexon, NEXON Games Co., Ltd.
