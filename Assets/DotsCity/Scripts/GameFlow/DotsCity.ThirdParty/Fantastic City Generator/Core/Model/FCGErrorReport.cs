#if FCG
using FCG;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.FCG
{
    public class FCGErrorReport : MonoBehaviour
    {
        [Serializable]
        public class ErrorData
        {
            public FCGWaypointsContainer Way1;
            public FCGWaypointsContainer Way2;
            public string Message;
        }

        public List<ErrorData> Errors = new List<ErrorData>();

        public bool TryToAddReport(GameObject parent, string message, FCGWaypointsContainer way1, FCGWaypointsContainer way2 = null)
        {
            ErrorData existReport = default;

            if (way2 == null)
            {
                existReport = Errors.Where(a => a.Way1 == way1 && a.Message == message).FirstOrDefault();
            }
            else
            {
                existReport = Errors.Where(a => a.Way1 == way1 && a.Way2 == way2 && a.Message == message).FirstOrDefault();
            }

            if (existReport == null)
            {
                Errors.Add(new ErrorData()
                {
                    Message = message,
                    Way1 = way1,
                    Way2 = way2
                });

                return true;
            }

            return false;
        }
    }
}
#endif