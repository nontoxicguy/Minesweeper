# summary
Some time ago, I liked minesweeper so I made a minesweer game. Then I decided it was a good idea to make an AI for this game with only base a non-tutorial Youtube video. After I liked that project so I made this repository. So here I am maintaining this spaghetti code base because it's fun.

If you want to contribute to this, you're welcome... if you achieve understanding my code.
# how it works
Want to contribute? First, thanks so much! Second, I'll give you hints on how the AI works.

The AI is working with a neural network system. It's going through empty tiles in the grid (tiles with *Images.Normal* as *Source*) with a number adjacent. It takes as input the tiles in a 9x9 square around the checked tile (excluding checked tile, 80 tiles) and gives as output the action realised for that square. Outputs and their actions are listed as below:

**0** - nothing  
**1** - reveal  
**2** - flag

We got 100 AIs and each one of them "tries" to reveal the most tiles possible to get the highest score. In the end every AI except the best one gets mixed with the best one and mutates. Repeat the process.

I've no idea if this thing works, I hope it does.
# requirements
To run the game you must have windows with .NET SDK 8.0 installed.

It is also worth noting that the repository got no .sln file. You will have to create one yourself and add the project to it. You can use
<br>`dotnet new sln`
<br>and
<br>`dotnet sln add Minesweeper/Minesweeper.csproj`
