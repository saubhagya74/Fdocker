import { HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';
import { environment } from '../../env';
@Injectable({
  providedIn: 'root',
})
export class VideoChatService {
  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('accessToken');
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  private hubUrl = `${environment.API_BASE_URL}/VideoChatHub`;
  public hubConnection!: HubConnection;

  public incomingCall = false;
  public remoteUserId = '';
  public isCallActive = false;

  public offerReceived = new BehaviorSubject<{
    senderId: string;
    offer: RTCSessionDescriptionInit;
  } | null>(null);

  public answerReceived = new BehaviorSubject<{
    senderId: string;
    answer: RTCSessionDescriptionInit;
  } | null>(null);

  public iceCandidateReceived = new BehaviorSubject<{
    senderId: string;
    candidate: RTCIceCandidateInit;
  } | null>(null);

  startConnection() {
    const token = localStorage.getItem('accessToken');
    if (!token) return;

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .catch((err) => console.error('Error while starting connection: ', err));

    this.hubConnection.on('ReceiveOffer', (senderId, offer) => {
      this.offerReceived.next({ senderId, offer: JSON.parse(offer) });
    });

    this.hubConnection.on('ReceiveAnswer', (senderId, answer) => {
      this.answerReceived.next({ senderId, answer: JSON.parse(answer) });
    });

    this.hubConnection.on('ReceiveIceCandidate', (senderId, candidate) => {
      this.iceCandidateReceived.next({
        senderId,
        candidate: JSON.parse(candidate),
      });
    });
  }

  sendOffer(receiverId: string, offer: RTCSessionDescriptionInit) {
    this.hubConnection.invoke('SendOffer', receiverId, JSON.stringify(offer));
  }
  sendAnswer(receiverId: string, answer: RTCSessionDescriptionInit) {
    this.hubConnection.invoke('SendAnswer', receiverId, JSON.stringify(answer));
  }
  sendIceCandidate(receiverId: string, candidate: RTCIceCandidate) {
    this.hubConnection.invoke(
      'SendIceCandidate',
      receiverId,
      JSON.stringify(candidate)
    );
  }
  sendEndCall(receiverId: string) {
    this.hubConnection.invoke('EndCall', receiverId);
  }
}
