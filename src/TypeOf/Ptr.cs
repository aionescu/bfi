namespace TypeOf {
  using System;

  public static class Ptr {
    public static Type TypeOf<T>() where T: unmanaged => typeof(T*);
  }
}