using System;
using System.Collections.ObjectModel;
using Vector.Tools;
using Vector.CANoe.Runtime;
using Vector.CANoe.Threading;
using Vector.Diagnostics;
using Vector.Scripting.UI;
using Vector.CANoe.TFS;
using Vector.CANoe.VTS;
using NetworkDB;

[TestClass]
public class MyTestClass
{
  const int kWAIT_TIMEOUT = 500; // 500msecs
  static NetworkDB.Frames.LockingState LockingSts = new NetworkDB.Frames.LockingState();
  
  [Export]
  [TestCase("Window Roll Down and Up","Checks all the positions of a window")]
  public static void ntc_WindowDownUp()
  {
    int WaitResult;
    
    NetworkDB.Frames.WindowState frame = new NetworkDB.Frames.WindowState();
    
    nf_PreConditions();
    
    Report.TestStep("Test Step 1", "Rolling down window completely");
    // For loop to roll down the window completely
    for(int LoopVar = 1; LoopVar < 16; LoopVar++)
    {
      Report.TestStep("Start","Roll Down Window to position " + LoopVar);
      //Set Window Request to Roll Down
      testNS.WindowRequest.Value = testNS.WindowRequest.Roll_Down;
      // Wait for the message “WindowState” to occur
      WaitResult = Execution.WaitForCANFrame<NetworkDB.Frames.WindowState>(ref frame,kWAIT_TIMEOUT);
      // Determine results of test step
      switch(WaitResult)
      {
        case 0: Report.TestStepFail("Message not received");
          break;
        case 1:
          // Check if Actual Window position = Calculated Window position
          if(frame.WindowPosition.Value == LoopVar)
            Report.TestStepPass("Window is rolled down to position " + frame.WindowPosition.Value);
          else
            Report.TestStepFail("Window is still in position " + frame.WindowPosition.Value);
          break;
        default: 
          Report.TestStepFail("Failed for other reasons");
          break;
      }
      // Make sure to reset Window Down button to original position
      testNS.WindowRequest.Value = testNS.WindowRequest.No_Request;
    }
    Report.AddWindowCapture("Trace", "" , "Window Down","");
    //traceWindowClear("Trace");
    Execution.Wait(kWAIT_TIMEOUT);
    //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    
    Report.TestStep("Test Step 2", "Rolling up window completely");
    // For loop to roll up the window completely
    for(int LoopVar = 14; LoopVar >= 0; LoopVar--)
    {
      Report.TestStep("Start","Roll Up Window to position " + LoopVar);
      //Set Window Request to Roll Up
      testNS.WindowRequest.Value = testNS.WindowRequest.Roll_Up;
      // Wait for the message “WindowState” to occur
      WaitResult = Execution.WaitForCANFrame<NetworkDB.Frames.WindowState>(ref frame,kWAIT_TIMEOUT);
      // Determine results of test step
      switch(WaitResult)
      {
        case 0: Report.TestStepFail("Message not received");
          break;
        case 1:
          // Check if Actual Window position = Calculated Window position
          if(frame.WindowPosition.Value == LoopVar)
            Report.TestStepPass("","Window is rolled up to position " + frame.WindowPosition.Value);
          else
            Report.TestStepFail("","Window is still in position " + frame.WindowPosition.Value);
          break;
        default: 
          Report.TestStepFail("Failed for other reasons");
          break;
      }
      // Make sure to reset Window Down button to original position
      testNS.WindowRequest.Value = testNS.WindowRequest.No_Request;
    }
    Report.AddWindowCapture("Trace", "" , "Window Up","");
  
  
  
    nf_PostConditions();
  }
  
  [Export]
  [TestCase("Request to Lock","Check if Lock/Unlock is performed when a request is sent")]
  public static void ntc_RequestLock()
  {
    int WaitResult;
    
    NetworkDB.Frames.LockingState frame1 = new NetworkDB.Frames.LockingState();

    nf_PreConditions();

    Report.TestStep("","Set request to Lock State");
    testNS.LockRq.Value = testNS.LockRq.RqToLock;

    Report.TestStep("Unlocking Test", "Set Locking request to RqToUnlock");
    testNS.LockRq.Value = testNS.LockRq.RqToUnlock;

    // Wait for the reply message from Doors ECU for max 500ms
    WaitResult = Execution.WaitForCANFrame<NetworkDB.Frames.LockingState>(ref frame1,kWAIT_TIMEOUT);

    // Determine results of test step
    switch(WaitResult)
    {
      case 0:
        Report.TestStepFail("Message not received");
        break;
      case 1:
        // If Lock state is unlocked, then test passed.
        if(frame1.LockState.Value == NetworkDB.LockState.Unlocked)
          Report.TestStepPass("Test Step 1", "Door is Unlocked");
        else
          Report.TestStepFail("Test Step 1", "Still Locked");
        break;
      default:
        break;
    }
    Execution.Wait(kWAIT_TIMEOUT); // Wait for 500ms

    Report.TestStep("Locking Test", "Set Locking request to RqToLock");
    testNS.LockRq.Value = testNS.LockRq.RqToLock;

    // Wait for the message “LockingSysState” to occur for 500ms
    WaitResult = Execution.WaitForCANFrame<NetworkDB.Frames.LockingState>(ref frame1,kWAIT_TIMEOUT);
    
    // Determine results of test step
    switch(WaitResult)
    {
      case 0:
        Report.TestStepFail("Message not received");
        break;
      case 1:
        // If Lock state is locked, then test passed.
        if(frame1.LockState.Value == NetworkDB.LockState.Locked )
          Report.TestStepPass("Test Step 1", "Door is Locked");
        else
          Report.TestStepFail("Test Step 1", "Still Unlocked");
        break;
      default:
        break;
    }
    Execution.Wait(kWAIT_TIMEOUT); // Wait for 500ms

    nf_PostConditions();
  }
  
  [Export]
  [TestCase("Auto Lock","Check if Lock is performed when velocity exceeds the threshold")]
  public static void ntc_AutoLock()
  {
    int WaitResult;
    
    NetworkDB.Frames.EngineStatus frame1 = new NetworkDB.Frames.EngineStatus();
    NetworkDB.Frames.LockingState frame2 = new NetworkDB.Frames.LockingState();

    nf_PreConditions();

    Report.TestStep("","Set Locking request to RqToUnlock");
    testNS.LockRq.Value = testNS.LockRq.RqToUnlock;

    //Check if the door is "Unlocked" before reaching the threshold
    Report.TestStep("Set speed","15 kmph");
    testNS.Velocity.Value = 15;
    WaitResult = Execution.WaitForCANFrame<NetworkDB.Frames.EngineStatus>(ref frame1, 200);

    if(frame1.Velocity.Value == 15)   
    {
      Execution.Wait(50);
      if(LockingSts.LockState.Value == NetworkDB.LockState.Unlocked)
        Report.TestStepPass("Door is in Unlocked state before reaching the speed threshold");
      else
        Report.TestStepFail("Door is not in Unlocked state before reaching the speed threshold");
    }
    //Check if the door is "Locked" after reaching the threshold
    Report.TestStep("Set speed","16 kmph");
    testNS.Velocity.Value = 16;
    WaitResult = Execution.WaitForCANFrame<NetworkDB.Frames.EngineStatus>(ref frame1, 200);

    if(frame1.Velocity.Value == 16)  
    {
      Execution.WaitForCANFrame<NetworkDB.Frames.LockingState>(ref frame2,200);
      if(frame2.LockState.Value == NetworkDB.LockState.Locked)
        Report.TestStepPass("Door is in Locked state after reaching the speed threshold");
      else
        Report.TestStepFail("Door is not in Locked state after reaching the speed threshold");
    }
    Report.TestStep("","Reset speed back to 0 kmph");
    testNS.Velocity.Value = 0;

    nf_PostConditions();
  }
  
  private static void nf_PreConditions()
  {
    Report.TestStep("Pre-cond","Start");
    Report.TestStep("Set Ignition to ON");
    // Set Ignition to ON
    testNS.IgnitionStart.Value = testNS.IgnitionStart.Ign_ON;
    Execution.Wait(500);
    Report.TestStep("Pre-cond","End");
  }
  
  private static void nf_PostConditions()
  {
    Report.TestStep("Post-cond","Start");
    Report.TestStep("Set Ignition to OFF");
    // Set Ignition to OFF
    testNS.IgnitionStart.Value = testNS.IgnitionStart.Ign_OFF;
    Execution.Wait(500);
    Report.TestStep("Post-cond","End");
  }
  
  //Required for Auto Lock testcase
  [OnCANFrame]
  public void DetectLockingFrame(NetworkDB.Frames.LockingState frame)
  {
    if(frame.ID == NetworkDB.Frames.LockingState.Definition.ID)
      LockingSts = frame;
    Output.WriteLine("Status ->" + LockingSts.LockState.Value);
  }
  
  //Function for printing the tester and setup related info
  [Export]
  [TestFunction]
  public static void nf_Preparation()
  {
    /* Add Information into Test engineer Information Table */
    Report.SetEngineerInfo("Company", "Vector Informatik Pvt. Ltd.");
    Report.SetEngineerInfo("Tester name", "Shyam Sundhar");
    Report.SetSetupInfo("CANoe", "Version 13.0");
    Report.SetSetupInfo("vTESTstudio", "Version 5.0");
    Report.SetSUTInfo ("SUT", "Doors ECU");
  }
}
