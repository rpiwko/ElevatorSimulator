using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace LiftSimulator
{
    /// <summary>    
    /// 1. Multithreading for elevators provided via ThreadPool class
    /// 2. Behaviour, when elevator was called:
    ///     - Use FindAllElevatorsWhichCanBeSent to pick elevators meeting any of following requirements:
    ///         - elevator is in its way to Passenger's floor (e.g. called by someone else)
    ///         - elevator is on different floor and its state is "Idle" 
    ///     - If list of available elevators is empty, do nothing
    /// 3. Manager has timer to periodcally (every 1000ms) check, if some floor doesn't need an elevator
    /// (which is signaled by Floor's LampUp and LampDown properties).
    /// </summary>
    public class ElevatorManager
    {
        #region FIELDS

        private readonly object locker = new object();
        private Elevator[] arrayOfAllElevators;
        public Elevator[] ArrayOfAllElevators
        {
            get;
            set;
        }

        private List<Elevator> listOfAllFreeElevators;

        private Floor[] arrayOfAllFloors;

        private System.Timers.Timer floorChecker;

        #endregion


        #region METHODS

        public ElevatorManager(Elevator[] ArrayOfAllElevators, Floor[] ArrayOfAllFloors)
        {
            //Initialize array with elevators
            this.arrayOfAllElevators = ArrayOfAllElevators;

            //Subscribe to elevators' events
            for (int i = 0; i < arrayOfAllElevators.Length; i++)
            {                
                arrayOfAllElevators[i].ElevatorIsFull += new EventHandler(ElevatorManager_ElevatorIsFull); //Subscribe to all ElevatorIsFull events
            }

            //Initialize array with floors
            this.arrayOfAllFloors = ArrayOfAllFloors;

            //Initialize list of free elevators
            this.listOfAllFreeElevators = new List<Elevator>();

            //Launch timer to periodically check, if some floor doesn't need an elevator
            this.floorChecker = new System.Timers.Timer(1000);
            this.floorChecker.Elapsed += new ElapsedEventHandler(this.ElevatorManager_TimerElapsed);
            this.floorChecker.Start();
        }

        public void PassengerNeedsAnElevator(Floor PassengersFloor, Direction PassengersDirection)
        {
            lock (locker)//Can be invoked from ElevatorManager thread or its timer thread
            {
                //Turn on appropriate lamp on the floor
                if (PassengersDirection == Direction.Up)
                {
                    PassengersFloor.LampUp = true;
                }
                else if (PassengersDirection == Direction.Down)
                {
                    PassengersFloor.LampDown = true;
                }

                //Search elevator
                FindAllElevatorsWhichCanBeSent(PassengersFloor, PassengersDirection);

                Elevator ElevatorToSend = ChooseOptimalElevatorToSend(PassengersFloor);

                if (ElevatorToSend != null)
                {
                    SendAnElevator(ElevatorToSend, PassengersFloor);
                }                
            }
        }

        private void FindAllElevatorsWhichCanBeSent(Floor PassengersFloor, Direction PassengersDirection)
        {
            listOfAllFreeElevators.Clear();

            //Find elevators in their way to Passenger's floor (e.g. called by someone else)
            for (int i = 0; i < arrayOfAllElevators.Length; i++)
            {
                //Get list of floors to visit
                List<Floor> ListOfFloorsToVisit = arrayOfAllElevators[i].GetListOfAllFloorsToVisit();

                //Check list of floors to visit                
                if (ListOfFloorsToVisit.Contains(PassengersFloor))
                {
                    listOfAllFreeElevators.Clear();
                    return; //Some elevator is already in its way, no need to send new one
                }
            }

            //Find elevators, which are idling now (do not moving anywhere)
            for (int i = 0; i < arrayOfAllElevators.Length; i++)
            {
                if (arrayOfAllElevators[i].GetElevatorStatus() == ElevatorStatus.Idle) 
                {
                    listOfAllFreeElevators.Add(arrayOfAllElevators[i]);
                }
            }
        }

        private Elevator ChooseOptimalElevatorToSend(Floor FloorWhereTheCallCameFrom)
        {
            //Check if listOfAllFreeElevators is not empty
            if (listOfAllFreeElevators.Count == 0)
            {
                return null;
            }

            //Return first elevator from the list
            return listOfAllFreeElevators[0];                
        }

        private void SendAnElevator(Elevator ElevatorToSend, Floor TargetFloor)
        {            
            ElevatorToSend.AddNewFloorToTheList(TargetFloor);

            //Create new thread and send the elevator
            ThreadPool.QueueUserWorkItem(delegate { ElevatorToSend.PrepareElevatorToGoToNextFloorOnTheList(); });            
        }

        #endregion


        #region EVENT HANDLERS

        public void ElevatorManager_TimerElapsed(object sender, ElapsedEventArgs e)
        {
            //Check if some floor doesn't need an elevator
            for (int i = 0; i < arrayOfAllFloors.Length; i++)
                {
                    if (arrayOfAllFloors[i].LampUp)
                    {
                        PassengerNeedsAnElevator(arrayOfAllFloors[i], Direction.Up);
                        Thread.Sleep(500); //delay to avoid sending two elevators at a time
                    }
                    else if(arrayOfAllFloors[i].LampDown)
                    {
                        PassengerNeedsAnElevator(arrayOfAllFloors[i], Direction.Down);
                        Thread.Sleep(500); //delay to avoid sending two elevators at a time
                    }   
                }
        }

        public void ElevatorManager_ElevatorIsFull(object sender, EventArgs e)
        {    
            //TO DO: Implement or remove!
        }
        
        #endregion EVENT HANDLERS
    }
}
