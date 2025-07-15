import { Component, Injectable, inject } from '@angular/core';
import {
  HttpClient,
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HTTP_INTERCEPTORS,
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

// Your models
interface UserDto {
  userName: string;
  password: string;
}

interface TokenResponseDto {
  accessToken: string;
  refreshToken: string;
}

// Interceptor (your version)
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  intercept(
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    const token = localStorage.getItem('accessToken');
    if (token) {
      const cloned = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
        },
      });
      return next.handle(cloned);
    }
    return next.handle(req);
  }
}

// AuthService (your version, inlined)
@Injectable()
export class AuthService {
  private http = inject(HttpClient);

  login(user: UserDto): Observable<TokenResponseDto> {
    return this.http.post<TokenResponseDto>(
      `http://localhost:5000/api/Auth/login`,
      user
    );
  }

  register(user: UserDto): Observable<any> {
    return this.http.post(`http://localhost:5000/api/Auth/register`, user);
  }
}

// Component (your version, no modifications)
@Component({
  selector: 'app-auth',
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [
    AuthService,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true,
    },
  ],
  templateUrl: './auth.component.html',
  styleUrls: ['./auth.component.css'],
})
export class AuthComponent {
  showLogin = true;

  user: UserDto = { userName: '', password: '' };
  error = '';
  success = '';

  constructor(private authService: AuthService, private router: Router) {}

  login() {
    this.authService.login(this.user).subscribe({
      next: (res) => {
        localStorage.setItem('accessToken', res.accessToken);
        localStorage.setItem('refreshToken', res.refreshToken);
        console.log('Login successful');
        this.router
          .navigate(['/chat'])
          .then((success) => {
            console.log('Navigation success:', success);
          })
          .catch((err) => {
            console.error('Navigation error:', err);
          });
      },
      error: (err) => {
        this.error = err.error;
      },
    });
  }

  register() {
    this.authService.register(this.user).subscribe({
      next: () => {
        this.success = 'Registration successful.';
        this.error = '';
      },
      error: (err) => {
        this.error = err.error;
        this.success = '';
      },
    });
  }
}
