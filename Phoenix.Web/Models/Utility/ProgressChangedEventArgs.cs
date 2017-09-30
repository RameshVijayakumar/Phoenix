using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phoenix.Web.Models
{
    public class ProgressChangedEventArgs : EventArgs
    {
        public string Message { get; private set; }
        public int ProgressPercentage { get; private set; }
        public string Status { get; private set; }
        public string Entity { get; private set; }
        public ProgressChangedEventArgs(int progressPercentage, string message, string status = null, string entity = null)
        {
            ProgressPercentage = progressPercentage;
            Message = message;
            Status = status;
            Entity = entity;
        }
    }
}