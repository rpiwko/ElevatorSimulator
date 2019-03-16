using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Timers;


namespace LiftSimulator
{
    public class Elevator
    {

        #region FIELDS

        private readonly object locker = new object();

        private Building myBuilding;

        private Floor currentFloor;
        private List<Floor> listOfFloorsToVisit;        
        private Direction elevatorDirection;
        private ElevatorStatus elevatorStatus;

        private int maximumPeopleInside;
        private List<Passenger> listOfPeopleInside;
        private bool IsFull;

        private Point elevatorPosition;
        private Bitmap[] elevatorFrames;
        private int currentFrameNumber;
        private int elevatorAnimationDelay;        
        private System.Timers.Timer elevatorTimer;

        #endregion


        #region METHODS

        public Elevator(Building Mybuilding, int HorizontalPosition, Floor StartingFloor)
        {
            this.myBuilding = Mybuilding;

            this.currentFloor = StartingFloor;
            this.listOfFloorsToVisit = new List<Floor>();
            this.elevatorDirection = Direction.None;
            this.elevatorStatus = ElevatorStatus.Idle;

            this.maximumPeopleInside = 2;
            this.listOfPeopleInside = new List<Passenger>();
            this.IsFull = false;

            this.elevatorPosition = new Point(HorizontalPosition, currentFloor.GetFloorLevelInPixels());
            currentFrameNumber = 0;
            elevatorFrames = new Bitmap[] 
            { 
                Properties.Resources.LiftDoors_Open, 
                Properties.Resources.LiftDoors_4, 
                Properties.Resources.LiftDoors_3,
                Properties.Resources.LiftDoors_2, 
                Properties.Resources.LiftDoors_1, 
                Properties.Resources.LiftDoors_Closed
            };
            this.elevatorAnimationDelay = 8;
            this.elevatorTimer = new System.Timers.Timer(6000); //set timer to 6 seconds
            this.elevatorTimer.Elapsed += new ElapsedEventHandler(this.Elevator_ElevatorTimerElapsed);

            this.PassengerEnteredTheElevator += new EventHandler(this.Elevator_PassengerEnteredTheElevator);
                        
            //Add new elevator to floor's list
            currentFloor.AddRemoveElevatorToTheListOfElevatorsWaitingHere(this, true);
        }

        public void PrepareElevatorToGoToNextFloorOnTheList()
        {
            //Method can be invoked from ElevatorManager thread (SendAnElevator()) or elevator's timer thread (Elevator_ElevatorTimerElapsed())
                        
            //Update elevator's status
            SetElevatorStatus(ElevatorStatus.PreparingForJob);
            
            //Disable the timer
            this.elevatorTimer.Stop();

            //Remove this elevator from current floor's list
            currentFloor.AddRemoveElevatorToTheListOfElevatorsWaitingHere(this, false);
            
            //Close the door
            this.CloseTheDoor();            

            //Go!
            GoToNextFloorOnTheList();            
        }

        private void GoToNextFloorOnTheList()
        {
            //Move control on the UI                 
            if (elevatorDirection == Direction.Down) //move down
            {
                this.SetElevatorStatus(ElevatorStatus.GoingDown);
                this.MoveTheElevatorGraphicDown(GetNextFloorToVisit().GetFloorLevelInPixels());
            }
            else if (elevatorDirection == Direction.Up) //move up
            {
                this.SetElevatorStatus(ElevatorStatus.GoingUp);
                this.MoveTheElevatorGraphicUp(GetNextFloorToVisit().GetFloorLevelInPixels());
            }

            //Update currentFloor
            this.currentFloor = GetNextFloorToVisit();

            //Remove current floor from the list of floors to visit
            this.listOfFloorsToVisit.RemoveAt(0);

            //Update elevator's direction
            UpdateElevatorDirection();

            //If one of passengers inside wants to get out here or this is end of the road,
            //then finalize going to next floor on the list
            if (SomePassengersWantsToGetOutOnThisFloor() || (this.elevatorDirection == Direction.None))
            {
                FinalizeGoingToNextFloorOnTheList();
                return;
            }

            //If elevator is not full, then check lamps on the floor
            if (!this.IsFull)
            {
                if (((this.elevatorDirection == Direction.Up) && (currentFloor.LampUp)) ||
                ((this.elevatorDirection == Direction.Down) && (currentFloor.LampDown)))
                {
                    FinalizeGoingToNextFloorOnTheList();
                    return;
                }
            }
            
            //If elevator doesn't stop here, let it go to next floor
            GoToNextFloorOnTheList();            
        }

        private void FinalizeGoingToNextFloorOnTheList()
        {
            //Reset appropriate lamp on current floor
            switch (this.elevatorDirection)
            {
                case Direction.Up:
                    currentFloor.LampUp = false;
                    break;
                case Direction.Down:
                    currentFloor.LampDown = false;
                    break;
                case Direction.None:
                    currentFloor.LampUp = false;
                    currentFloor.LampDown = false;
                    break;
                default:
                    break;
            }
            
            //Open the door
            this.OpenTheDoor();

            //Update elevator's status
            SetElevatorStatus(ElevatorStatus.WaitingForPassengersToGetInAndGetOut);

            //Inform all passengers inside
            List<Passenger> PassengersInsideTheElevator = new List<Passenger>(listOfPeopleInside);
            foreach (Passenger SinglePassengerInsideTheElevator in PassengersInsideTheElevator)
            {
                SinglePassengerInsideTheElevator.ElevatorReachedNextFloor();
                Thread.Sleep(SinglePassengerInsideTheElevator.GetAnimationDelay() * 40); //to make sure all passengers will be visible when leaving the building
            }            

            //Add this elevator to next floor's list
            currentFloor.AddRemoveElevatorToTheListOfElevatorsWaitingHere(this, true);

            //Rise an event on current floor to inform passengers, who await
            currentFloor.OnElevatorHasArrivedOrIsNoteFullAnymore(new ElevatorEventArgs(this));

            //Enable the timer            
            this.elevatorTimer.Start();
        }
        
        public void AddNewFloorToTheList(Floor FloorToBeAdded)
        {
            lock (locker) //Method can be invoked from ElevatorManager thread (SendAnElevator()) or passenger's thread (AddNewPassengerIfPossible())
            {
                //If FloorToBeAdded is already on the list, do nothing
                if(GetListOfAllFloorsToVisit().Contains(FloorToBeAdded))
                {
                    return;
                }

                //If elevator is going up
                if (this.currentFloor.FloorIndex < FloorToBeAdded.FloorIndex)
                {
                    for (int i = this.currentFloor.FloorIndex + 1; i <= FloorToBeAdded.FloorIndex; i++)
                    {
                        if (!GetListOfAllFloorsToVisit().Contains(myBuilding.ArrayOfAllFloors[i]))
                        {
                            GetListOfAllFloorsToVisit().Add(myBuilding.ArrayOfAllFloors[i]);
                        }
                    }
                }

                //If elevator is going down
                if (this.currentFloor.FloorIndex > FloorToBeAdded.FloorIndex)
                {
                    for (int i = this.currentFloor.FloorIndex - 1; i >= FloorToBeAdded.FloorIndex; i--)
                    {
                        if (!GetListOfAllFloorsToVisit().Contains(myBuilding.ArrayOfAllFloors[i]))
                        {
                            this.GetListOfAllFloorsToVisit().Add(myBuilding.ArrayOfAllFloors[i]);
                        }
                    }
                }

                //Update ElevatorDirection
                UpdateElevatorDirection();                
            }
        }

        private bool SomePassengersWantsToGetOutOnThisFloor()
        {
            foreach (Passenger PassengerInsideThElevator in listOfPeopleInside)
            {
                if (PassengerInsideThElevator.GetTargetFloor() == this.currentFloor)
                {
                    return true;
                }                
            }
            return false;
        }

        public Floor GetCurrentFloor()
        {
            return currentFloor;
        }

        private Floor GetNextFloorToVisit()
        {
            lock (locker) //To avoid e.g. adding new element and checking whole list at the same time
            {
                if (listOfFloorsToVisit.Count > 0)
                {
                    return this.listOfFloorsToVisit[0];
                }
                else
                {
                    return null;
                }
            }
        }

        public List<Floor> GetListOfAllFloorsToVisit()
        {
            lock (locker) //To avoid e.g. adding new element and checking whole list at the same time
            {
                return listOfFloorsToVisit;
            }
        }

        private void UpdateElevatorDirection()
        {
            //Lock not needed:
            //AddNewFloorToTheList method is the only reference for this method and it has its own lock         
            if (GetNextFloorToVisit() == null)
            {
                this.elevatorDirection = Direction.None;
                return;
            }

            if (currentFloor.FloorIndex < GetNextFloorToVisit().FloorIndex)
            {
                this.elevatorDirection = Direction.Up;
            }
            else
            {
                this.elevatorDirection = Direction.Down;
            }            
        }

        public bool AddNewPassengerIfPossible(Passenger NewPassenger, Floor TargetFloor)
        {
            //Passengers are added synchronically. Lock not needed.

            if (!IsFull && //check, if there is a place for another passenger
                ((GetElevatorStatus() == ElevatorStatus.Idle) || (GetElevatorStatus() == ElevatorStatus.WaitingForPassengersToGetInAndGetOut)))
            {
                //Reset elevator timer, so the passenger has time to get in
                this.ResetElevatorTimer();

                this.listOfPeopleInside.Add(NewPassenger); //add new passenger
                this.AddNewFloorToTheList(TargetFloor); //add new floor                    
                if (this.listOfPeopleInside.Count >= this.maximumPeopleInside) //set flag, if needed
                {
                    this.IsFull = true;
                    this.SetElevatorStatus(ElevatorStatus.PreparingForJob); // to prevent other passengers attempt to get in
                }

                return true; //new passenger added successfully
            }
            else
                return false; //new passenger not added due to lack of space in the elevator            
        }

        public void RemovePassenger(Passenger PassengerToRemove)
        {
            lock (locker) //Can be invoked by multiple passengers at once
            {
                this.listOfPeopleInside.Remove(PassengerToRemove);
                this.IsFull = false;
            }
        }

        public void ResetElevatorTimer()
        {
            lock (locker)
            {
                this.elevatorTimer.Stop();
                this.elevatorTimer.Start();
            }
        }

        private void MoveTheElevatorGraphicDown(int DestinationLevel)
        {
            for (int i = this.GetElevatorYPosition(); i <= DestinationLevel; i++)
            {
                Thread.Sleep(this.elevatorAnimationDelay);
                this.elevatorPosition = new Point(GetElevatorXPosition(), i);
            }
        }

        private void MoveTheElevatorGraphicUp(int DestinationLevel)
        {
            for (int i = this.GetElevatorYPosition(); i >= DestinationLevel; i--)
            {
                Thread.Sleep(this.elevatorAnimationDelay);
                this.elevatorPosition = new Point(GetElevatorXPosition(), i);
            }
        }

        private void CloseTheDoor()
        {
            for (int i = 0; i < 5; i++)
            {
                switch (this.currentFrameNumber)
                {
                    case (0):
                        this.currentFrameNumber = 1;
                        Thread.Sleep(100);
                        break;
                    case(1):
                        this.currentFrameNumber = 2;
                        Thread.Sleep(100);
                        break;
                    case(2):
                        this.currentFrameNumber = 3;
                        Thread.Sleep(100);
                        break;
                    case(3):
                        this.currentFrameNumber = 4;
                        Thread.Sleep(100);
                        break;
                    case(4):
                        this.currentFrameNumber = 5;
                        Thread.Sleep(100);
                        break;
                }                
            }
        }

        private void OpenTheDoor()
        {
            for (int i = 0; i < 5; i++)
            {
                switch (this.currentFrameNumber)
                {
                    case (5):
                        this.currentFrameNumber = 4;
                        Thread.Sleep(100);
                        break;
                    case (4):
                        this.currentFrameNumber = 3;
                        Thread.Sleep(100);
                        break;
                    case (3):
                        this.currentFrameNumber = 2;
                        Thread.Sleep(100);
                        break;
                    case (2):
                        this.currentFrameNumber = 1;
                        Thread.Sleep(100);
                        break;
                    case (1):
                        this.currentFrameNumber = 0;
                        Thread.Sleep(100);
                        break;
                }
            }
        }

        public int GetElevatorXPosition()
        {
            return this.elevatorPosition.X;
        }

        public int GetElevatorYPosition()
        {
            return this.elevatorPosition.Y;
        }

        public Bitmap GetCurrentFrame()
        {
            return this.elevatorFrames[currentFrameNumber];
        }

        public ElevatorStatus GetElevatorStatus()
        {
            lock (locker) //To avoid e.g. setting and getting status at the same time
            {
                return this.elevatorStatus;
            }
        }

        private void SetElevatorStatus(ElevatorStatus Status)
        {
            lock (locker) //To avoid e.g. setting and getting status at the same time
            {
                this.elevatorStatus = Status;
            }
        }

        public Direction GetElevatorDirection()
        {
            lock (locker) //To avoid reading during updating the elevatorDirection
            {
                return elevatorDirection;
            }
        }
        
        #endregion


        #region EVENTS

        public event EventHandler PassengerEnteredTheElevator;
        public void OnPassengerEnteredTheElevator(PassengerEventArgs e)
        {
            EventHandler passengerEnteredTheElevator = PassengerEnteredTheElevator;
            if (passengerEnteredTheElevator != null)
            {
                passengerEnteredTheElevator(this, e);
            }
        }

        public event EventHandler ElevatorIsFull;
        public void OnElevatorIsFullAndHasToGoDown(EventArgs e)
        {
            EventHandler elevatorIsFull = ElevatorIsFull;
            if (elevatorIsFull != null)
            {
                elevatorIsFull(this, e);
            }
        }

        #endregion


        #region EVENT HANDLERS

        public void Elevator_PassengerEnteredTheElevator(object sender, EventArgs e)
        {
            //Restart elevator's timer
            ResetElevatorTimer();
        }

        public void Elevator_ElevatorTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (GetNextFloorToVisit() == null)
            {
                elevatorTimer.Stop();
                SetElevatorStatus(ElevatorStatus.Idle);                
            }
            else
            {
                //ThreadPool.QueueUserWorkItem(delegate { this.PrepareElevatorToGoToNextFloorOnTheList(); });                
                this.PrepareElevatorToGoToNextFloorOnTheList();
            }
        }

        #endregion

    }
}
