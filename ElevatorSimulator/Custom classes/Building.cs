using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LiftSimulator
{
    public class Building
    {
        #region FIELDS

        private Floor[] arrayOfAllFloors; //list of all floors (needed e.g. for NewPassengerButtons)
        public Floor[] ArrayOfAllFloors
        {
            get { return arrayOfAllFloors; }
            private set { }
        }

        private Elevator[] arrayOfAllElevators; //list of all elvators
        public Elevator[] ArrayOfAllElevators
        {
            get { return arrayOfAllElevators; }
            private set { }
        }

        private int exitLocation;
        public int ExitLocation
        {
            get { return exitLocation; }
            private set { }
        }

        public List<Passenger> ListOfAllPeopleWhoNeedAnimation;

        public ElevatorManager ElevatorManager;

        #endregion FIELDS


        #region METHODS

        public Building()
        {
            //Set exitLocation on 0 floor
            exitLocation = 677;

            //Initialize floors
            arrayOfAllFloors = new Floor[4];
            arrayOfAllFloors[0] = new Floor(this, 0, 373);
            arrayOfAllFloors[1] = new Floor(this, 1, 254);
            arrayOfAllFloors[2] = new Floor(this, 2, 144);
            arrayOfAllFloors[3] = new Floor(this, 3, 32);

            //Initialize elevators (each elevator starts on randomly choosen floor)
            arrayOfAllElevators = new Elevator[3];
            Random random = new Random();
            
            arrayOfAllElevators[0] = new Elevator(this, 124, arrayOfAllFloors[random.Next(arrayOfAllFloors.Length)]);
            arrayOfAllElevators[1] = new Elevator(this, 210, arrayOfAllFloors[random.Next(arrayOfAllFloors.Length)]);
            arrayOfAllElevators[2] = new Elevator(this, 295, arrayOfAllFloors[random.Next(arrayOfAllFloors.Length)]);

            //Initialize list of all people inside (to track who's inside and need to be animated)
            ListOfAllPeopleWhoNeedAnimation = new List<Passenger>();

            //Initialize ElevatorManager object
            ElevatorManager = new ElevatorManager(ArrayOfAllElevators, ArrayOfAllFloors);
        }

        #endregion METHODS
    }
}
