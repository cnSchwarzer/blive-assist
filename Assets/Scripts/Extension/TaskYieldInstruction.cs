using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Extension {
    public static class TaskYieldInstructionExtension {
        public static TaskYieldInstruction AsCoroutine(this Task task) {
            return new TaskYieldInstruction(task);
        }
        public static TaskYieldInstruction AsCoroutine<T>(this Task<T> task) {
            return new TaskYieldInstruction<T>(task);
        }
        
        public static TaskYieldInstruction AsCoroutine(this ValueTask task) {
            return new TaskYieldInstruction(task.AsTask());
        }
        public static TaskYieldInstruction AsCoroutine<T>(this ValueTask<T> task) {
            return new TaskYieldInstruction<T>(task.AsTask());
        }
    } 
    
    public class TaskYieldInstruction : CustomYieldInstruction
    {
        public Task Task { get; private set; }

        public bool Error => Task.Exception != null;

        public override bool keepWaiting
        {
            get {
                if (Task.Exception != null)
                    return false;
                
                return !Task.IsCompleted;
            }
        }

        public TaskYieldInstruction(Task task)
        {
            Task = task ?? throw new ArgumentNullException("task");
        } 
    }

    public class TaskYieldInstruction<T> : TaskYieldInstruction
    {
        public new Task<T> Task { get; private set; }

        public T Result
        {
            get { return Task.Result; }
        }

        public TaskYieldInstruction(Task<T> task)
            : base(task)
        {
            Task = task;
        }
    }
}