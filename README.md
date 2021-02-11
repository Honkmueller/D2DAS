# D2DAS
Diablo II - Donation assisted speedrun
This tool is a trainer, that applies buffs to your Diablo II character. These buffs are triggered by donations. In this case tips from a connected StreamElements account.
The tool also provides a little web server that serves an overlay for OBS.
# D2DAS v0.5 setup and user guide
## Prerequisites
- a working copy of DII - LoD 1.14d
- a recent version of Cheat Engine (6.0+, tested with 7.2) - [https://www.cheatengine.org/downloads.php](https://www.cheatengine.org/downloads.php)
- a StreamElements account - [https://streamelements.com/](https://streamelements.com/)
## Setup (settings.json):
-  **AccountID + JWTToken** <- StreamElements authorization ([https://docs.streamelements.com/docs](https://docs.streamelements.com/docs))
   -  **Hashtags** <- controls, what a certain hashtag in the message of tip will cause
   -  **Type (Health, Mana, Gold, Speed)** <- what status should be (de)buffed in the game
   - **ValueType (Percentage, Total)** <- is the applied value a total amount or a percentage of the amount at the time of application
   - **Effect (Relative, Absolute)** <- is the Value added or substracted OR will the chosen status be set to the Value
   - **Value [integer]** <- the value to be applied.
     - You can use the following syntax to specify a dependency to the donation: "[v]/$[d]" (value [v] per donated dollar [d])
     - You also can set it to "max".
     - This takes the highest observed value of the chosen status when the ValueType is Total OR 100%, if the ValueType is Percentage.
     - It only really makes sense as a total buff for health and mana.
   - **Duration [integer]** <- the duration of the (de)buff in seconds.
     - You can use the following syntax to specify a dependency to the donation: "[t]/$[d]" (time [t] per donated dollar [d])
## About hashtags
Hashtags are case insesitive. The donor has to assure correct spelling, though.
If the donor puts in two or more valid hashtags only one will be applied. It's not necessarily the first so the best advice is to just use one per donation.
## About the inner workings of (de)buffs
- per type only one buff can be active at any certain time
- new buffs of a type, of which a buff is already active will be queued
- there is a grace period/cool down of one second between buffs
- a buff always fixes the value of the chosen status to its value for its duration. So keep in mind:
- if you want a buff to simply add/substract a certain value, set the duration to 0 and set the Effect to "Relative"
- fixed means fixed. If health has a buff you can neither be hurt nor heal yourself
## Using the tool
1. Start or load a game in DII: LoD
2. Start Cheat Engine, connect do Diablo II and load the provided cheat table (cheat-table/D2LoD1.14d.CT)
3. Check the first check box named "Enable pointer scan"
4. If you started a new game, sell a random item
5. In Cheat Engine right-click on "Enable pointer scan" and select "Copy"
6. Open D2DAS.exe as Administrator
7. Click both of the two buttons on the top in D2DAS
8. Open "[http://localhost:4711](http://localhost:4711)" in a web browser or add it as a browser source in OBS.
9. When you add the page as browser source in OBS add the following custom CSS:
```css
body { background-color: rgba(0, 0, 0, 0); margin: 0px auto; overflow: hidden; }
*{color: white; }
```

The check for new donations will begin immediately.
If you just want to test it you can select a hashtag, put in a dollar amount and click "Send Tip" to test it.
## Closing
To avoid a BLUE SCREEN OF DEATH please clode D2DAS and Cheat Engine BEFORE you close Diablo 2. It doesn't have to happen, but it happened to me, once.
We are still messing with the computers memory at the end of the day.
## Things, which are a bit fishy right now
- every donation will be treated as dollar. Even if it's rubels or whatever
- the StreamElements API for some reason treats the donation amount as an integer. God only knows what happens, when somebody send $1.50