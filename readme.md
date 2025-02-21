# Elden Ring automatic FPS unlocker

#### _Simple and small mod to unlock Elden Ring framerate, that when opened automatically launches game. <br/> **Fully compatible with DLC and Seamless Coop mod**_

_C# game patching logic and memory patterns from [Elden Ring FPS Unlocker and more](https://github.com/uberhalit/EldenRingFpsUnlockAndMore) and its [pull request #147](https://github.com/uberhalit/EldenRingFpsUnlockAndMore/pull/147) respecting the original license._

## Mod download

_See the [release section](https://github.com/luca2040/EldenRingAutoFPSUnlocker/releases)_

## Description

- This mod does not modify any file, and it **doesn't require any installation or configuration, as it is a single small executable file and does everything automatically**.
- When the mod is opened, it automatically checks if Steam is running to correctly launch the game, if not it automatically starts it, and then starts the game.
- After launching the game the mod will show a little overlay at the top-left of the screen for some seconds, saying the framerate limit that the game has been unlocked to.
- If the game is already opened and the mod is launched, it will automatically apply the framerate unlock to the current opened game, without needing to close it and restart.

_If you want to start the game without the mod, launch it as usual because the mod does not permanently modify the game._

## Framerate set overlay (example)

![overlay](https://github.com/user-attachments/assets/9e5a463e-7b81-4140-9c50-3a32ced44ecc)

## Configuration

_Before changing the config make sure to have started the mod at least once._<br/><br/>
Even though this mod doesn't require to be configured, you can customize some settings.<br/>
To do this, open the game folder:<br/>
To open it select Elden Ring from your Steam library, and select `Manage` (The gear icon on the right), then go on `Manage` and select `Browse local files`.
In the folder that will open go in `Game`.<br/>
In the game folder open the folder `EldenRingAutoFPSUnlocker` and inside it you will find a `config.ini` file containing the mod configuration (You can edit it simply using Windows's notepad).<br/>
In that file you will see the different config options with a description:

- `target_framerate`: Set the framerate the game will be unlocked to, if set to `-1` the mod will automatically set this based on your monitor.
- `confirm_overlay`: Choose whether to show the overlay with the set FPS for a few seconds when the game is started.
- `use_seamless_coop_mod`: If the mod detects that Seamless Coop is installed, it will launch the game using it.
- `ensure_steam_opened`: Check if Steam is opened before launching the game and in case start it. **Setting this to False may cause the game to not start automatically.**
- `minimize_steam_windows` and `close_steam_windows`: Automatically minimize or close Steam windows before launching the game.

## Usage

Simply place this mod's file anywhere you want in your pc and open it to launch the game with unlocked FPS.<br/><br/>
_The first time using the mod it may ask for admin rights to find the game files._

## Possible problems

If the game is set to `fullscreen` it may still lock to 60 FPS so you have to override this in the NVIDIA Control panel settings (For NVIDIA) or Display settings (For AMD) or set another option like `borderless window` in the game.<br/>
To remove the FPS limit from the NVIDIA Control panel go to `3D settings` -> `Program settings`, if in the dropdown `Select a program to customize` there isn't Elden Ring, click on `Add` and select it. Then in the `Specify the settings for this program` set the option `Vertical sync` to `Off`<br/><br/>
If the mod doesn't work as it should or you have some suggestions, you are free to open a new Issue or Pull Request.
