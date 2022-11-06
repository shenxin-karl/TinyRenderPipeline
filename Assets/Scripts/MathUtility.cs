using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtility {
   public static int DivideByMultiple(int value, int alignment) {
      return Math.Max((value + alignment - 1) / alignment, 1);
   }
}
