import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  email = '';
  password = '';
  errorMessage = '';
  isLoading = false;

  constructor(private authService: AuthService, private router: Router) {}

  onSubmit() {
    if (!this.email || !this.password) {
      this.errorMessage = 'Please enter email and password.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.login(this.email, this.password).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.error || 'Authentication failed. Please check your credentials.';
      }
    });
  }

  loginWithGoogle() {
    this.isLoading = true;
    this.errorMessage = '';
    
    // Simulate OAuth handshake
    setTimeout(() => {
      this.authService.oauthLogin('Google', 'google_mock_token_abc123', 'google_user@gmail.com', 'Google Agent User').subscribe({
        next: (res) => {
          this.isLoading = false;
          this.router.navigate(['/dashboard']);
        },
        error: (err) => {
          this.isLoading = false;
          this.errorMessage = err.error?.error || 'Google login failed.';
        }
      });
    }, 1000);
  }

  loginWithGitHub() {
    this.isLoading = true;
    this.errorMessage = '';

    // Simulate OAuth handshake
    setTimeout(() => {
      this.authService.oauthLogin('GitHub', 'github_mock_token_xyz789', 'github_user@github.com', 'GitHub Agent User').subscribe({
        next: (res) => {
          this.isLoading = false;
          this.router.navigate(['/dashboard']);
        },
        error: (err) => {
          this.isLoading = false;
          this.errorMessage = err.error?.error || 'GitHub login failed.';
        }
      });
    }, 1000);
  }
}
