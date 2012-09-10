using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace LiftSimulator
{
    public class Floor
    {
        #region FIELDS

        private readonly object locker = new object();

        private Building myBuilding;

        private int maximumAmmountOfPeopleInTheQueue; // depends on graphic

        private Passenger[] arrayOfPeopleWaitingForElevator;

        private List<Elevator> listOfElevatorsWaitingHere;

        private int floorIndex; //possible values for current graphic: 0, 1, 2, 3
        public int FloorIndex
        {
            get { return floorIndex; }
            private set { }
        }

        private int floorLevel; //determines (in pixels) where passengers should stand; depends on building graphic

        private int beginOfTheQueue; //determines, where queue of paasengers begins; depends on building graphic

        private int widthOfSlotForSinglePassenger; //ammount of pixels reserved for single passenger; depends on passenger graphic        

        public bool LampUp; //indicates, that at least one of passengers wants to up
        public bool LampDown; //indicates, that at least one of passengers wants to down

        #endregion


        #region METHODS

        public Floor(Building myBuilding, int floorNumber, int floorLevel)
        {
            this.myBuilding = myBuilding;

            maximumAmmountOfPeopleInTheQueue = 8; //only 8 passengers at once can be visible in current layout
            this.arrayOfPeopleWaitingForElevator = new Passenger[maximumAmmountOfPeopleInTheQueue];
            this.floorIndex = floorNumber;

            listOfElevatorsWaitingHere = new List<Elevator>();

            //Initialize variables, which depend on graphics:
            this.floorLevel = floorLevel;
            beginOfTheQueue = 367;
            widthOfSlotForSinglePassenger = 46;

            //Turn off both lamps
            LampUp = false;
            LampDown = false;
        }

        private int? FindFirstFreeSlotInQueue()
        {
            //Lock not needed. Only one reference, already locked.
            for (int i = 0; i < maximumAmmountOfPeopleInTheQueue; i++)
            {
                if (arrayOfPeopleWaitingForElevator[i] == null)
                {
                    return i;
                }
            }

            return null;
        }

        private void AddRemoveNewPassengerToTheQueue(Passenger PassengerToAddOrRemvove, bool AddFlag)
        {
            //Lock not needed. Only two references (from this), both already locked                        
            if (AddFlag) //Add passenger
            {
                int? FirstFreeSlotInQueue = FindFirstFreeSlotInQueue(); //Make sure there is a space to add new passenger
                if (FirstFreeSlotInQueue != null)
                {
                    //Add passenger object to an array                    
                    this.arrayOfPeopleWaitingForElevator[(int)FirstFreeSlotInQueue] = PassengerToAddOrRemvove;

                    //Add passenger control to the UI
                    int NewPassengerVerticalPosition = this.beginOfTheQueue + (this.widthOfSlotForSinglePassenger * (int)FirstFreeSlotInQueue);
                    PassengerToAddOrRemvove.PassengerPosition = new Point(NewPassengerVerticalPosition, GetFloorLevelInPixels());

                    //Add passenger to Building's list
                    myBuilding.ListOfAllPeopleWhoNeedAnimation.Add(PassengerToAddOrRemvove);
                }
            }
            else //Remove passenger
            {
                int PassengerToRemoveIndex = Array.IndexOf<Passenger>(GetArrayOfPeopleWaitingForElevator(), PassengerToAddOrRemvove);
                this.GetArrayOfPeopleWaitingForElevator()[PassengerToRemoveIndex] = null;
            }            
        }

        public void AddRemoveElevatorToTheListOfElevatorsWaitingHere(Elevator ElevatorToAddOrRemove, bool AddFlag)
        {
            lock (locker) //Few elevators can try to add/remove themselfs at the same time
            {
                if (AddFlag) //Add elevator
                {
                    //Add elevator to the list
                    listOfElevatorsWaitingHere.Add(ElevatorToAddOrRemove);

                    //Subscribe to an event, rised when passenger entered the elevator
                    ElevatorToAddOrRemove.PassengerEnteredTheElevator += new EventHandler(this.Floor_PassengerEnteredTheElevator);
                }
                else //Remove elevator
                {
                    //Remove elevator from the list
                    listOfElevatorsWaitingHere.Remove(ElevatorToAddOrRemove);

                    //Unsubscribe from an event, rised when passenger entered the elevator
                    ElevatorToAddOrRemove.PassengerEnteredTheElevator -= this.Floor_PassengerEnteredTheElevator;
                }
            }
        }

        public int GetMaximumAmmountOfPeopleInTheQueue()
        {
            return maximumAmmountOfPeopleInTheQueue;
        }

        public int GetCurrentAmmountOfPeopleInTheQueue()
        {
            lock (locker) //The same lock is on add/remove passenger to the queue
            {
                int CurrentAmmountOfPeopleInTheQueue = 0;
                for (int i = 0; i < maximumAmmountOfPeopleInTheQueue; i++)
                {
                    if (this.arrayOfPeopleWaitingForElevator[i] != null)
                    {
                        CurrentAmmountOfPeopleInTheQueue++;
                    }
                }
                return CurrentAmmountOfPeopleInTheQueue;
            }
        }

        public Passenger[] GetArrayOfPeopleWaitingForElevator()
        {
            return arrayOfPeopleWaitingForElevator;
        }

        public List<Elevator> GetListOfElevatorsWaitingHere()
        {
            //Lock not needed. Method for passengers only.
            lock (locker)
            {
                return this.listOfElevatorsWaitingHere;
            }
        }

        public int GetFloorLevelInPixels()
        {
            return this.floorLevel;
        }

        #endregion


        #region EVENTS

        public event EventHandler NewPassengerAppeared;
        public void OnNewPassengerAppeared(EventArgs e)
        {
            EventHandler newPassengerAppeared = NewPassengerAppeared;
            if (newPassengerAppeared != null)
            {
                newPassengerAppeared(this, e);
            }
        }

        public event EventHandler ElevatorHasArrivedOrIsNotFullAnymore;
        public void OnElevatorHasArrivedOrIsNoteFullAnymore(ElevatorEventArgs e)
        {
            EventHandler elevatorHasArrivedOrIsNoteFullAnymore = ElevatorHasArrivedOrIsNotFullAnymore;
            if (elevatorHasArrivedOrIsNoteFullAnymore != null)
            {
                elevatorHasArrivedOrIsNoteFullAnymore(this, e);
            }
        }

        #endregion


        #region EVENT HADNLERS

        public void Floor_NewPassengerAppeared(object sender, EventArgs e)
        {
            lock (locker)
            {
                //Unsubscribe from this event (not needed anymore)
                this.NewPassengerAppeared -= this.Floor_NewPassengerAppeared;

                Passenger NewPassenger = ((PassengerEventArgs)e).PassengerWhoRisedAnEvent;

                AddRemoveNewPassengerToTheQueue(NewPassenger, true);
            }
        }

        public void Floor_PassengerEnteredTheElevator(object sender, EventArgs e)
        {
            lock (locker)
            {
                Passenger PassengerWhoEnteredOrLeftTheElevator = ((PassengerEventArgs)e).PassengerWhoRisedAnEvent;

                //Remove passenger from queue                
                AddRemoveNewPassengerToTheQueue(PassengerWhoEnteredOrLeftTheElevator, false);
            }
        }

        #endregion
    }
}
