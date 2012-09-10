using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiftSimulator
{
    public class PassengerEventArgs : EventArgs
    {
        public Passenger PassengerWhoRisedAnEvent;        

        public PassengerEventArgs(Passenger PassengerWhoRisedAnEvent)
        {
            this.PassengerWhoRisedAnEvent = PassengerWhoRisedAnEvent;
        }
    }
}
