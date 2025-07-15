import {
  Component,
  ElementRef,
  inject,
  ViewChild,
  OnInit,
  Input,
  Output,
  EventEmitter,
} from '@angular/core';
import { VideoChatService } from '../services/video-chat.service';

@Component({
  selector: 'app-video-chat',
  standalone: true,
  imports: [],
  templateUrl: './video-chat.component.html',
  styleUrls: ['./video-chat.component.css'],
})
export class VideoChatComponent implements OnInit {
  @Input() remoteUserId!: string;
  @Output() close = new EventEmitter<void>();
  @ViewChild('localVideo', { static: true })
  localVideo!: ElementRef<HTMLVideoElement>;
  @ViewChild('remoteVideo', { static: true })
  remoteVideo!: ElementRef<HTMLVideoElement>;

  private peerConnection: RTCPeerConnection | null = null;
  private iceCandidateQueue: RTCIceCandidateInit[] = [];
  private remoteDescriptionSet = false;
  signalRService = inject(VideoChatService);

  ngOnInit(): void {
    this.setupPeerConnection();
    this.setupSignalListener();
    this.signalRService.startConnection();
  }

  setupSignalListener() {
    this.signalRService.hubConnection.on('CallEnded', () => {
      this.endCall();
    });

    this.signalRService.answerReceived.subscribe(async (data) => {
      if (data && data.answer) {
        await this.peerConnection?.setRemoteDescription(
          new RTCSessionDescription(data.answer)
        );
        this.remoteDescriptionSet = true;
        // Add any queued ICE candidates
        this.iceCandidateQueue.forEach((candidate) => {
          this.peerConnection?.addIceCandidate(new RTCIceCandidate(candidate));
        });
        this.iceCandidateQueue = [];
      }
    });

    this.signalRService.iceCandidateReceived.subscribe(async (data) => {
      if (data && data.candidate) {
        if (this.remoteDescriptionSet) {
          await this.peerConnection?.addIceCandidate(
            new RTCIceCandidate(data.candidate)
          );
        } else {
          this.iceCandidateQueue.push(data.candidate);
        }
      }
    });
  }
  declineCall() {
    this.signalRService.incomingCall = false;
    this.signalRService.isCallActive = false;
    this.signalRService.sendEndCall(this.remoteUserId);
    this.close.emit();
  }

  async acceptCall() {
    this.signalRService.incomingCall = false;
    this.signalRService.isCallActive = true;

    let offer = this.signalRService.offerReceived.getValue()?.offer;
    if (offer) {
      await this.peerConnection?.setRemoteDescription(
        new RTCSessionDescription(offer)
      );
      this.remoteDescriptionSet = true;
      // Add any queued ICE candidates
      this.iceCandidateQueue.forEach((candidate) => {
        this.peerConnection?.addIceCandidate(new RTCIceCandidate(candidate));
      });
      this.iceCandidateQueue = [];
      let answer = await this.peerConnection?.createAnswer();
      if (answer) {
        await this.peerConnection?.setLocalDescription(answer);
        this.signalRService.sendAnswer(this.remoteUserId, answer);
      }
    }
  }

  async startCall() {
    this.signalRService.isCallActive = true;
    let offer = await this.peerConnection?.createOffer();
    if (offer) {
      await this.peerConnection?.setLocalDescription(offer);
      this.signalRService.sendOffer(this.remoteUserId, offer);
    }
  }
  setupPeerConnection() {
    this.peerConnection = new RTCPeerConnection({
      iceServers: [
        { urls: 'stun:stun.l.google.com:19302' },
        { urls: 'stun:stun.services.mozilla.com' },
      ],
    });

    this.peerConnection.onicecandidate = (event) => {
      if (event.candidate) {
        this.signalRService.sendIceCandidate(
          this.remoteUserId,
          event.candidate
        );
      }
    };

    this.peerConnection.ontrack = (event) => {
      this.remoteVideo.nativeElement.srcObject = event.streams[0];
    };

    navigator.mediaDevices
      .getUserMedia({ video: true, audio: true })
      .then((stream) => {
        this.localVideo.nativeElement.srcObject = stream;
        stream
          .getTracks()
          .forEach((track) => this.peerConnection?.addTrack(track, stream));
      })
      .catch((error) => console.error('Error accessing media devices.', error));
    this.remoteDescriptionSet = false;
    this.iceCandidateQueue = [];
  }

  async startLocalVideo() {
    const stream = await navigator.mediaDevices.getUserMedia({
      video: true,
      audio: true,
    });
    this.localVideo.nativeElement.srcObject = stream;
    stream.getTracks().forEach((track) => {
      if (this.peerConnection) {
        this.peerConnection.addTrack(track, stream);
      }
    });
  }

  async endCall() {
    if (this.peerConnection) {
      this.signalRService.isCallActive = false;
      this.signalRService.incomingCall = false;
      this.peerConnection.close();
      this.peerConnection = null;
      if (this.localVideo && this.localVideo.nativeElement.srcObject) {
        const stream = this.localVideo.nativeElement.srcObject as MediaStream;
        stream.getTracks().forEach((track) => track.stop());
        this.localVideo.nativeElement.srcObject = null;
      }
      this.signalRService.sendEndCall(this.remoteUserId);
      this.close.emit();
    }
  }
}
