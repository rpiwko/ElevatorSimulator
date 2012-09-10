----------
I Overview
----------

Multi-threaded and scalable elevator simulation written in C# (Windows Forms).


----------
II Project
----------

1. Project consists of five custom clasess:
- Building class
	- Represents the building where entire simulation takes place (one instance for project).
	- Holds references to all Floor objects, Elevator objects and Passenger objects.
	- Holds reference to ElevatorManager.
	- Defines exit location (pixels, depends on graphic).
	- Each Floor, Elevator and Passenger has reference to Building object.
- Floor class
	- Represents a single floor (few instances are allowed).
	- Each floor starts on randomly choosen floor.
	- Defines maximum people in the queue (depends on graphic).
	- Defines begin of the queue (pixels, depends on graphic).
	- Defines ammount of space for single passenger waiting for elevator (pixels, depends on graphic).
	- Defines level, where elevator should stop (pixels, depends on graphic).	
	- Has two lamps, indicated in which direction passengers, who awaits, want to travel (up and/or down).
	- Each instance holds references to all its elevators and passengers.		
- Elevator class
	- Represents a single elevator (few instances are allowed).
	- Defines maximum people single elevator can carry.
	- Has internal timer. When time elapses, door are closed and elevator moves to next floor on its list of floors to visit. Timer is reset every time, when new passenger is added to the elevator (but before passenger's animation is actually played).
	- When it's full, it won't react for a new calls.
	- Has Direction property to inform other objects (passengers and/or elevator manager) where it is heading.
	- Has elevatorAnimationDelay property which determines animation speed (door animation and move animation).
- Passenger class 
	- Represents a single passenger (few instances are allowed).
	- Passenger graphic is randomly chosen from ArrayOfAllPassengerGraphics.
	- When new passenger is created, it checks whether there is an elevator on his/her floor. If elevator is available, its direction is OK and it's not full, then passenger will enter. If not, he/she will call for a new elevator.
	- Has passengerAnimationDelay property which determines move animation speed.
- ElevatorManager 
	- Manages all elevators (one instance for project).
	- In current (default) implementation:
		- When elevator was called manager uses FindAllElevatorsWhichCanBeSent() to pick elevators meeting any of following requirements:
			- Elevator is on its way to Passenger's floor (e.g. called by someone else).
			- Elevator is on different floor and its state is "Idle".
		- If list of available elevators is empty, do nothing.
		- Periodcally (every 1000ms) check, if some floor doesn't need an elevator (which is signaled by Floor's LampUp and LampDown properties).

2. Events and event handlers
Floor:
	Events:
		- NewPassengerAppeared		
		- ElevatorHasArrivedOrIsNotFullAnymore		
	Event handlers:
		- Floor_NewPassengerAppeared()
		- Floor_PassengerEnteredTheElevator()
		
Elevator:
	Events:
		- PassengerEnteredTheElevator
		- ElevatorIsFull	
	Event handlers:
		- Elevator_ElevatorTimerElapsed()
		- Elevator_PassengerEnteredTheElevator()
		
Passeneger
	Event handlers:
		- Passenger_NewPassengerAppeared() 
		- Passenger_ElevatorHasArrivedOrIsNoteFullAnymore()

3. Graphic & animation are handled by Form1 class. When timerRefresh timer ticks, form is invalidated and the following five methods are launched:
	- PaintBuilding()
	- PaintElevators()
	- PaintPassengers()
For smooth animation Interval property for timerRefresh was set for 10ms (100 frames per second).


---------------
III Scalability
---------------

1. To add/remove elevator
- Update your building graphic (Resources\Building.jpg).
- Update main form size.
- Edit Building.cs contructor:
	- Update exitLocation
	- Update arrayOfAllElevators size
	- Add/remove elevator to/from arrayOfAllElevators
	- Adjust others' elevators positions

2. To add/remove floor
- Update your building graphic (Resources\Building.jpg).
- Update main form:
	- Update form size
	- Add/remove "New passenger" button. If button was added, remember to update its FloorIndex property (via Designer).
- Edit Building.cs contructor:
	- Update exitLocation;
	- Update arrayOfAllFloors size
	- Add/remove flor to/from arrayOfAllFloors
	- Adjust others' floors levels

3. To increase/decrease maximum people in the elevator: 
- Go to Elevetar.cs class
- Find contructor
- Change maximumPeopleInside value

4. To increase/decrease animation speed:
- Go to Elevator.cs class
- Find contructor
- Change elevatorAnimationDelay value
- Go to Passenger.cs
- Find constructor
- Change passengerAnimationDelay value

5. To increase/decrease maximum queue length:
- Go to Floor.cs class
- Find contructor 
- Adjust maximumAmmountOfPeopleInTheQueue value
- Adjust beginOfTheQueue (depends on building graphic)
- Adjust widthOfSlotForSinglePassenger (depends on building and passengers graphics)
	

-----------------
IV Multithreading
-----------------

By default application uses ThreadPool, but it should also work with manually created threads as well. Simply hit CTRL+F, find all code sections with "ThreadPool" and edit them.


----------
V Licence
----------

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the 'Software'), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.






