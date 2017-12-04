using System;
using System.Collections.Generic;
using System.Linq;

namespace XcstTodo {

   public class Todo {

      public int id { get; set; }
      public int? order { get; set; }
      public string title { get; set; }
      public bool completed { get; set; }
   }

   public class TodoDatabase {

      static List<Todo> Todos = new List<Todo>();
      static readonly object padlock = new object();

      public IEnumerable<Todo> GetAll() {
         lock (padlock) {
            return Todos.ToArray();
         }
      }

      public Todo Find(int id) {
         lock (padlock) {
            return Todos.FirstOrDefault(t => t.id == id);
         }
      }

      public void Add(Todo todo) {

         lock (padlock) {
            Todos.Add(todo);
            todo.id = Todos.Count;
         }
      }

      public void Update(Todo todo) { }

      public void Remove(int id) {
         lock (padlock) {
            Todos.RemoveAll(t => t.id == id);
         }
      }

      public void RemoveAll() {
         lock (padlock) {
            Todos.Clear();
         }
      }
   }
}
