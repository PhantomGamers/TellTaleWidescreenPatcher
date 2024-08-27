[//]: # "why tf is the second T in Telltale capital"
# Telltale Widescreen Patcher

This program patches your Telltale games to run in ultrawide aspect ratios (16:10, 21:9, and 32:9)! No more "cinematic" black
bars all over your screen.

## Instructions

Simply run the program, browse to the executable for your Telltale game, and click Patch!  
If you get an error, your game is not supported. Please open an issue and tell me the game you tried & where you got it
from (GOG/Steam).

**Make sure you run the game and select your resolution before patching.**

## Tested Titles
[//]: # "really need to add/test more games here, i.e. the exapanse, twd 1 & 2, poker night 1 & 2, bttf, strong bad's cgfap, etc, unless this patch doesnt work on them, idk I have a normal 16:9 screen, Jurassic park def tho, I can see the patch in the code so that must mean it works right?"

 Game Name                                        | Issues                                                                                                                                                                              
--------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
 Batman: The Telltale Series                      | Black bars, [use Rose's Fix as well as this](https://community.pcgamingwiki.com/files/file/1729-batman-the-telltale-series-no-letterbox/)
 Batman: The Enemy Within                         | Played through 100%, works
 Guardians of the Galaxy: The Telltale Series     | Played through 100%, works
 Jurassic Park: The Game                          | None found (credit to @RoseTheFlower for adding support)
 Minecraft: Story Mode                            | Doesn't work, use Flawless Widescreen's The Wolf Among Us patch
 Minecraft: Story Mode - Season Two               | None found
 Tales from the Borderlands                       | Doesn't work, use Flawless Widescreen's The Wolf Among Us patch
 The Walking Dead: Michonne                       | Might crash on startup, cause unknown.
 The Walking Dead: A New Frontier                 | None found
 The Walking Dead: The Final Season               | None found
 The Wolf Among Us                                | No longer works after an update, use [SUWSF Patch](https://github.com/PhantomGamers/TellTaleWidescreenPatcher/files/15126502/The.Wolf.Among.Us.SUWSF.v2.zip) or Flawless Widescreen
 The Walking Dead: The Telltale Definitive Series | Played through 100%, works
 Game of Thrones                                  | Doesn't work, use Flawless Widescreen's The Wolf Among Us patch
 Sam & Max: Save the World (Remastered)           | None found
 Sam & Max: Beyond Time and Space (Remastered)    | None found
 Sam & Max: The Devil's Playhouse (Remastered)    | None found
 Tales of Monkey Island                           | None found

## Todo

Allow custom input for AR/Resolution values

## Dependencies

* .NET Framework 4.8

## Credits

* [@mrexodia for the pattern scanning code](https://github.com/mrexodia/PatternFinder)
* [@atom0s for the Steamless patcher!](https://github.com/atom0s/Steamless)
* [Armin1702 from WSGF for the inspiration.](http://www.wsgf.org/forums/viewtopic.php?f=95&t=31782)
* [@RoseTheFlower from WSGF for testing and finding the function for Jurassic Park!](https://github.com/RoseTheFlower)
* Hardes from the Unofficial Metro Patch discord for helping me test.
* Telltale for making this necessary in the first place!
