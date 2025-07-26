import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import * as signalR from '@microsoft/signalr';
import { Router } from '@angular/router';
import { HttpHeaders, HttpClient, HttpParams } from '@angular/common/http';
import { OnInit } from '@angular/core';
import { VideoChatComponent } from '../video-chat/video-chat.component';

interface MessageDto {
  senderId: string;
  receiverId: string;
  content: string;
  timeStamp: string;
}

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, VideoChatComponent],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.css'],
})
export class ChatComponent implements OnInit {
  private hubConnection: signalR.HubConnection | undefined;
  constructor(private router: Router, private http: HttpClient) {}

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('accessToken');
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  currentUserName: string | null = null;
  currentUserId: string | null = null;

  ngOnInit(): void {
    const token = localStorage.getItem('accessToken');
    if (!token) return;

    const payload = JSON.parse(atob(token.split('.')[1]));
    this.currentUserName =
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'];
    this.currentUserId =
      payload[
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
      ];

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.API_BASE_URL}/ChatHub`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          return retryContext.previousRetryCount < 10 ? 10000 : null;
        },
      })
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('✅ SignalR Connected'))
      .catch((err) => console.error('❌ Connection error:', err));
    console.log(`currentusername:${this.currentUserName}`);
    console.log(`currentuserId:${this.currentUserId}`);
    this.hubConnection.on(
      'ReceiveMessage',
      (senderName: string, content: string) => {
        const timeNow = new Date().toISOString();
        const isMyMessage = senderName === this.currentUserName;
        this.messages.push({
          content,
          senderId: isMyMessage ? this.currentUserId! : this.receiverId,
          receiverId: isMyMessage ? this.receiverId : this.currentUserId!,
          timeStamp: timeNow,
        });
      }
    );

    setInterval(() => {
      if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
        this.hubConnection.invoke('Ping');
      }
    }, 200000);
    this.loadUsers();
  }

  loadUsersReturned: { userName: string; userId: string }[] | null = null;
  loadUsersErrors: string | null = null;

  loadUsers(): void {
    const headers = this.getAuthHeaders();
    this.http
      .get<{ userName: string; userId: string }[]>(
        `${environment.API_BASE_URL}/Chat/loadusers`,
        { headers }
      )
      .subscribe({
        next: (data) => {
          this.loadUsersReturned = data;
          this.loadUsersErrors = null;
        },
        error: (err) => {
          this.loadUsersReturned = null;
          this.loadUsersErrors = err.error;
        },
      });
  }
  error: string | null = null;
  receiverId: string = '';
  messageContent: string = '';
  receiverName: string = '';

  showVideoChat: boolean = false;

  startVideoCall() {
    if (!this.receiverId) {
      alert('Please select a user to start a video call.');
      return;
    }
    this.showVideoChat = true;
  }

  closeVideoChat() {
    this.showVideoChat = false;
  }

  selectUser(user: { userName: string; userId: string }) {
    console.log('👤 Selected user:', user);
    this.receiverName = user.userName;
    this.receiverId = user.userId;
    this.messages = [];
    this.loadMessages();
    // Optionally, close video chat if switching users
    this.showVideoChat = false;
  }

  sendMessage() {
    //we are getting senderusername and message content from sendmessage function in backedn
    //we gotta send userid and message content
    console.log(`receiverid:${this.receiverId}`);
    console.log(typeof this.receiverId);
    console.log(`receiverName:${this.receiverName}`);
    console.log(typeof this.receiverName);
    if (this.receiverName && this.messageContent && this.hubConnection) {
      //this needs just the receivername, it will find other data in the backend
      this.hubConnection
        .invoke('SendMessage', this.receiverName, this.messageContent) //change here too
        .then(() => {
          this.messageContent = ''; // Don't push the message here
        })
        .catch((err) => console.error('Send failed:', err));
    }
  }
  initiateMessagebool: boolean = false;
  initiateMessageContent: string = '';
  initiateMessage() {
    this.receiverName = this.searchedName;
    this.messageContent = this.initiateMessageContent;
    this.sendMessage();
  }

  messages: MessageDto[] = [];
  loadMessages() {
    if (!this.receiverId) return;

    const headers = this.getAuthHeaders();

    this.http
      .get<MessageDto[]>(
        `${environment.API_BASE_URL}/Chat/loadmessage/${this.receiverId}`,
        { headers }
      )
      .subscribe({
        next: (data) => {
          this.messages = data.map((m) => ({
            ...m,
            timeStamp: m.timeStamp.endsWith('Z')
              ? m.timeStamp
              : m.timeStamp + 'Z',
          }));
          console.log('🌐 loadMessages response:', data); // replace with the full history
        },
        error: (err) => {
          console.error('Failed to load messages', err);
        },
      });
  }

  // lasttime: any | null = null;
  // loadMessages() {
  //   if (!this.receiverId) {
  //     console.warn('No receiver selected — skipping history load');
  //     return;
  //   }

  //   const headers = this.getAuthHeaders();
  //   let params = new HttpParams();

  //   // Ensure timestamp is always ISO UTC (ending in 'Z')
  //   let timestampToSend = this.lasttime ?? new Date().toISOString();
  //   if (!timestampToSend.endsWith('Z')) {
  //     timestampToSend = new Date(timestampToSend).toISOString();
  //   }

  //   params = params.set('date', timestampToSend);

  //   this.http
  //     .get<MessageDto[]>(
  //       `https://localhost:7083/Chat/loadmessage/${this.receiverId}`,
  //       {
  //         headers,
  //         params,
  //       }
  //     )
  //     .subscribe({
  //       next: (data) => {
  //         this.messages = [...this.messages, ...data];
  //         this.error = null;

  //         if (data.length > 0) {
  //           const oldest = data[data.length - 1].timeStamp;

  //           // Normalize timestamp to UTC ISO string
  //           this.lasttime = oldest.endsWith('Z')
  //             ? oldest
  //             : new Date(oldest).toISOString();
  //         }
  //       },
  //       error: (err) => console.error('❌ Failed to load messages', err),
  //     });
  // }
  //last time to store the last message i.e the oldest message in the current instance , saving that time and sending to the endponint in backedn for if we want to load again , from the exact place where we left from ,

  // getCurrentUserName(): string | null {
  //   const token = localStorage.getItem('accessToken');
  //   if (!token) return null;

  //   try {
  //     const payload = JSON.parse(atob(token.split('.')[1]));
  //     return payload['name']; // or actual claim key
  //   } catch {
  //     return null;
  //   }
  // }
  searchReturned: any | null = null;
  searchedName = '';
  searchError: string | null = null;

  searchUser() {
    const headers = this.getAuthHeaders();
    this.http
      .get(`${environment.API_BASE_URL}/chat/searchUser/${this.searchedName}`, {
        headers,
      })
      .subscribe({
        next: (data) => {
          this.searchReturned = data;
          this.searchError = null;
        },
        error: (err) => {
          this.searchReturned = null;
          this.searchError = err.error?.message || err.error || 'Unknown error';
        },
      });
  }
  profileReturned: any | null = null;
  profileerror: string | null = null;

  seeProfile() {
    console.log('you cliked see profile button');
    const headers = this.getAuthHeaders();
    this.http
      .get(`${environment.API_BASE_URL}/chat/seeprofile`, { headers })
      .subscribe({
        next: (data) => {
          this.profileReturned = data;
          this.profileerror = null;
        },
        error: (err) => {
          this.profileReturned = null;
          this.profileerror = err.error;
        },
      });
  }

  RequestReturned: any | null = null;
  RequestReturnedError: any | null = null;

  sendRequest(requesttoid: string) {
    const headers = this.getAuthHeaders();
    console.log('you clikced send request');
    this.http
      .get(`${environment.API_BASE_URL}/chat/sendrequest/${requesttoid}`, {
        headers,
      })
      .subscribe({
        next: (data) => {
          this.RequestReturned = data;
          this.RequestReturnedError = null;
        },
        error: (err) => {
          this.RequestReturned = null;
          this.RequestReturnedError = err.error;
        },
      });
  }
  NotificationReturned: any[] | null = null;
  NotificationReturnedError: any | null = null;

  seeNotification() {
    console.log('you pressed notification button');
    const headers = this.getAuthHeaders();
    this.http
      .get<any[]>(`${environment.API_BASE_URL}/chat/seenotification`, {
        headers,
      })
      .subscribe({
        next: (data) => {
          this.NotificationReturned = data;
          this.NotificationReturnedError = null;
        },
        error: (err) => {
          this.NotificationReturned = null;
          this.NotificationReturnedError = err.error;
        },
      });
  }

  acceptReturned: any | null = null;
  acceptReturnedError: any | null = null;
  acceptrequest(accpettoid: string, statuschange: boolean) {
    const headers = this.getAuthHeaders();
    this.http
      .get(
        `${environment.API_BASE_URL}/chat/acceptordeclinerequest/${accpettoid}/${statuschange}`,
        { headers }
      )
      .subscribe({
        next: (data) => {
          this.acceptReturned = data;
          this.acceptReturnedError = null;
        },
        error: (err) => {
          this.acceptReturned = null;
          this.acceptReturnedError = err.error;
        },
      });
  }
}
