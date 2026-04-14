import { useLocation, Link } from "react-router-dom";
import { useEffect } from "react";
import { Button } from "@/components/ui/button";
import { Home, ArrowLeft } from "lucide-react";

const NotFound = () => {
  const location = useLocation();

  useEffect(() => {
    console.error("404 Error: User attempted to access non-existent route:", location.pathname);
  }, [location.pathname]);

  return (
    <div className="flex min-h-[calc(100vh-4rem)] items-center justify-center px-4">
      <div className="absolute inset-0 -z-10 overflow-hidden pointer-events-none">
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 h-[600px] w-[600px] rounded-full bg-primary/3 blur-3xl" />
      </div>

      <div className="text-center animate-fade-scale max-w-md">
        <div className="relative mb-6 inline-block">
          <span className="font-heading text-[8rem] md:text-[10rem] font-bold leading-none text-foreground/5 select-none">
            404
          </span>
          <div className="absolute inset-0 flex items-center justify-center">
            <span className="font-heading text-5xl md:text-6xl font-bold gradient-text">
              404
            </span>
          </div>
        </div>
        <h1 className="text-2xl font-heading font-bold mb-3">Page not found</h1>
        <p className="text-muted-foreground mb-8">
          The page you&apos;re looking for doesn&apos;t exist or has been moved.
        </p>
        <div className="flex gap-3 justify-center">
          <Button onClick={() => window.history.back()} variant="outline" className="gap-2">
            <ArrowLeft className="h-4 w-4" /> Go back
          </Button>
          <Button asChild className="gap-2">
            <Link to="/"><Home className="h-4 w-4" /> Home</Link>
          </Button>
        </div>
      </div>
    </div>
  );
};

export default NotFound;
