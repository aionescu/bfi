namespace PtrHelper {
  using System;

  public static class Ptr<T> where T: unmanaged {
    public static Type TypeOf => typeof(T*);
  }
}