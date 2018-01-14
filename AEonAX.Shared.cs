using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace AEonAX.Shared
{
    public static class Extn
    {
        /// <summary>
        /// Simple helper extension method to marshall to correct
        /// thread if its required
        /// </summary>
        /// <param name="control">The source control like "Textbox1"</param>
        /// <param name="methodcall">The method to call like "() => Textbox1.text="Zango";"</param>
        /// <param name="priorityForCall">The thread priority</param>
        public static void WPFUIize(
            this DispatcherObject control,
            Action methodcall,
            DispatcherPriority priorityForCall = DispatcherPriority.ApplicationIdle)
        {
            //see if we need to Invoke call to Dispatcher thread
            if (control.Dispatcher.Thread != Thread.CurrentThread)
                control.Dispatcher.Invoke(priorityForCall, methodcall);
            else
                methodcall();
        }
    }
    public class NotifyBase : INotifyPropertyChanged
    {
        /// <summary>
        /// This is the implementation of INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property. 
        // The CallerMemberName Attribute below is new to .NET Framework 4.5.  
        //It determines the calling property name automatically!

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }





    //DoSomethingCommand = new SimpleCommand
    //            {
    //                ExecuteDelegate = this.RunCommand
    //            };


    //DoSomethingCommand = new SimpleCommand
    //            {
    //                ExecuteDelegate = o => this.SelectedItem = o,
    //                CanExecuteDelegate = o => o != null
    //            };

    public class SimpleCommand : SimpleCommand<object> { };
    public class SimpleCommand<T> : ICommand
    {

        /// <summary>
        /// Bool Function to Call
        /// </summary>
        public Predicate<T> CanExecuteDelegate { get; set; }

        /// <summary>
        /// Call Bool Function
        /// </summary>
        public Action<T> ExecuteDelegate { get; set; }

        #region ICommand Members
        public bool CanExecute(object parameter)
        {
            if (CanExecuteDelegate != null)
                return CanExecuteDelegate((T)parameter);
            return true;// if there is no can execute default to true
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            ExecuteDelegate?.Invoke((T)parameter);
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="X">Predicate<X></typeparam>
    /// <typeparam name="Y">Action<Y></typeparam>
    public class ComplexCommand<X, Y> : ICommand
    {
        public Predicate<X> CanExecuteDelegate { get; set; }
        public Action<Y> ExecuteDelegate { get; set; }

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            if (CanExecuteDelegate != null)
                return CanExecuteDelegate((X)parameter);
            return true;// if there is no can execute default to true
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            ExecuteDelegate?.Invoke((Y)parameter);
        }
        #endregion
    }




}
