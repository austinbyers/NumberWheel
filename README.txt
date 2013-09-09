NumberWheel 1.2 - README
Austin Byers, 2013


Overview
-------------------------
The NumberWheel is a Visual C# program that was sponsored by the University of Chicago for research purposes.
(In fact, it was part of the same research project as my CogAssess repo.)
The researchers needed a way to visually generate a random numnber on the computer, and the solution
we came up with was a spinning wheel, much like the Wheel of Fortune. The participant clicks on the wheel,
and it spins and then slows and stops on a randomly chosen number. 

The heart of the program is the GameWheel class, which can be easily added to any C# application.
The class provides methods to create, spin, and draw the wheel, as well to set the graphics surface.
A GameWheel consists of an array of Sectors, which are defined by their text, angle, and color.
The GameWheel currently supports all 10 digits and the uppercase letters A - H.


Program Use
-------------------------
To use the program, just run the included "SpinningWheel.exe" file.
This illustrates two different uses of the GameWheel class: 
One wheel consists of the numbers 50 - 1,000 in increments of 25, and the other wheel illustrates 
how to use letters on the wheel as well as numbers.


C# Implementation
-------------------------
Each character has to be drawn "manually" by the computer in order for it to be able to rotate. By "manually,"
I mean that every 55 ms, the program calculates the position and rotation of every character on the wheel before
drawing it accordingly. Each character is drawn with respect to its center (i.e. start at the center and pick a point
10 pixels up, then draw 15 pixels left, etc). It can also be confusing to keep track of the direction of rotation.
For example, a lot of the built-in graphics functions draw clockwise, but math functions assume counter-clockwise
for their calculations.

Juggling all of this requires quite a bit of math, which is what makes this so cool!


Future Development
-------------------------
With a little bit more work, this could turn into an incredibly flexible and powerful application.
The two main possibilities I see for future development are:

	- (1) add support for the remaining letters of the alphabet
	- (2) add more color options and gradients

I probably won't be working on this anytime soon (unless people express interest in it), 
but anyone is welcome to pick up where I left off!


License
-------------------------
This can be used for anything you want; all I ask is that you give credit where credit is due!
Also feel free to email me and let me know how you put the wheel to good use! It's always encouraging
to know that my code occasionally sees the light of day :)
