import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { VideoChatService } from './services/video-chat.service';
import { MatDialog } from '@angular/material/dialog';
import { VideoChatComponent } from './video-chat/video-chat.component';
@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent implements OnInit {
  title = 'FE-C';

  private signalRService = inject(VideoChatService);
  private Dialog = inject(MatDialog);

  ngOnInit(): void {
    const token = localStorage.getItem('accessToken');
    if (!token) return;
    this.signalRService.startConnection();
  }

  startOfferReceive() {
    this.signalRService.offerReceived.subscribe(async (data) => {
      if (data) {
        this.Dialog.open(VideoChatComponent, {
          width: '400px',
          height: '600px',
          disableClose: false,
        });
        this.signalRService.remoteUserId = data.senderId;
        this.signalRService.incomingCall = true;
      }
    });
  }
}
